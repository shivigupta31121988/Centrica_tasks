using Assets.Domain.MeterData;
using Assets.Domain.Settlement;
using Assets.Infrastructure.MeterData;
using Assets.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assets.Infrastructure.Settlement;

/// <summary>
/// Core settlement engine, shared by both API routes. All amounts are
/// DKK, day/month boundaries follow local (Europe/Copenhagen) time with
/// DST per the confirmed decision, and results are rounded to 2 decimal
/// places using banker's rounding (MidpointRounding.ToEven).
///
/// LOGGING: the per-hour formula and inputs (production, price, computed
/// amount) are logged at Debug level via LogDebug - this is deliberate:
/// Debug-level logs are suppressed by the default Production logging
/// configuration (see appsettings.json vs appsettings.Development.json),
/// so this detailed trail is available for debugging in dev but doesn't
/// spam production logs. A one-line LogInformation summary per call is
/// always logged regardless of environment.
/// </summary>
public sealed class SettlementCalculator
{
    private readonly IAssetRepository _assetRepository;
    private readonly IMeterReadingRepository _meterReadingRepository;
    private readonly ISpotPriceProvider _spotPriceProvider;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<SettlementCalculator> _logger;

    public SettlementCalculator(
        IAssetRepository assetRepository,
        IMeterReadingRepository meterReadingRepository,
        ISpotPriceProvider spotPriceProvider,
        IOptions<SettlementSettings> settings,
        ILogger<SettlementCalculator> logger)
    {
        _assetRepository = assetRepository;
        _meterReadingRepository = meterReadingRepository;
        _spotPriceProvider = spotPriceProvider;
        _timeZone = ResolveTimeZone(settings.Value.TimeZoneId);
        _logger = logger;
    }

    /// <summary>Returns null if the asset id doesn't exist (caller maps to 404).</summary>
    public async Task<IReadOnlyList<DailySettlement>?> CalculateForAssetAsync(
        string assetId, DateOnly localStart, DateOnly localEnd, CancellationToken ct = default)
    {
        var asset = await _assetRepository.GetByIdAsync(assetId, ct);
        if (asset is null)
        {
            return null;
        }

        var (utcStart, utcEndExclusive) = ToUtcRange(localStart, localEnd);

        var readings = await _meterReadingRepository.GetByMeterPointIdInRangeAsync(asset.MeterPointId, utcStart, utcEndExclusive, ct);
        var prices = await _spotPriceProvider.GetPricesAsync(utcStart, utcEndExclusive, ct);

        var productionByHour = BucketProductionByHour(readings);
        var priceByHour = prices.ToDictionary(p => p.StartUtc, p => p.PricePerKwhDkk);

        var byDate = new Dictionary<DateOnly, (decimal Amount, int Incomplete)>();

        for (var hour = FloorToHour(utcStart); hour < utcEndExclusive; hour = hour.AddHours(1))
        {
            ct.ThrowIfCancellationRequested();

            var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(hour, _timeZone));
            var bucket = byDate.TryGetValue(localDate, out var existing) ? existing : (Amount: 0m, Incomplete: 0);

            var hasReading = productionByHour.TryGetValue(hour, out var production);
            var hasPrice = priceByHour.TryGetValue(hour, out var price);

            if (hasReading && hasPrice)
            {
                var amount = production * price;
                _logger.LogDebug(
                    "Settlement calc [asset]: assetId={AssetId} meterPointId={MeterPointId} hourUtc={HourUtc} localDate={LocalDate} " +
                    "formula=production*price production={Production}kWh price={Price}DKK/kWh amount={Amount}DKK",
                    assetId, asset.MeterPointId, hour, localDate, production, price, amount);

                byDate[localDate] = (bucket.Amount + amount, bucket.Incomplete);
            }
            else
            {
                _logger.LogDebug(
                    "Settlement calc [asset]: assetId={AssetId} meterPointId={MeterPointId} hourUtc={HourUtc} localDate={LocalDate} " +
                    "INCOMPLETE hasReading={HasReading} hasPrice={HasPrice} - hour excluded from amount",
                    assetId, asset.MeterPointId, hour, localDate, hasReading, hasPrice);

                byDate[localDate] = (bucket.Amount, bucket.Incomplete + 1);
            }
        }

        var result = byDate
            .OrderBy(kv => kv.Key)
            .Select(kv => new DailySettlement(kv.Key, Round(kv.Value.Amount), kv.Value.Incomplete))
            .ToList();

        _logger.LogInformation(
            "Settlement calculated for asset {AssetId} ({MeterPointId}), {Start} to {End}: {DayCount} day(s), total {Total} DKK",
            assetId, asset.MeterPointId, localStart, localEnd, result.Count, Round(result.Sum(r => r.Amount)));

        return result;
    }

    public async Task<IReadOnlyList<MonthlySettlement>> CalculateTotalAsync(
        DateOnly localStart, DateOnly localEnd, CancellationToken ct = default)
    {
        var (utcStart, utcEndExclusive) = ToUtcRange(localStart, localEnd);

        var readings = await _meterReadingRepository.GetAllInRangeAsync(utcStart, utcEndExclusive, ct);
        var prices = await _spotPriceProvider.GetPricesAsync(utcStart, utcEndExclusive, ct);

        // Summed across every meter point sharing an hour - see class
        // summary re: "missing reading" being ambiguous at the aggregate
        // level (a subset of assets can legitimately have zero readings
        // without that being an error). Missing PRICE is still tracked.
        var productionByHour = BucketProductionByHour(readings);
        var priceByHour = prices.ToDictionary(p => p.StartUtc, p => p.PricePerKwhDkk);

        var byMonth = new Dictionary<(int Year, int Month), (decimal Amount, int Incomplete)>();

        for (var hour = FloorToHour(utcStart); hour < utcEndExclusive; hour = hour.AddHours(1))
        {
            ct.ThrowIfCancellationRequested();

            var localDateTime = TimeZoneInfo.ConvertTimeFromUtc(hour, _timeZone);
            var key = (localDateTime.Year, localDateTime.Month);
            var bucket = byMonth.TryGetValue(key, out var existing) ? existing : (Amount: 0m, Incomplete: 0);

            var hasPrice = priceByHour.TryGetValue(hour, out var price);
            productionByHour.TryGetValue(hour, out var totalProduction); // 0 if nobody reported this hour

            if (hasPrice)
            {
                var amount = totalProduction * price;
                _logger.LogDebug(
                    "Settlement calc [total]: hourUtc={HourUtc} year={Year} month={Month} " +
                    "formula=totalProduction*price totalProduction={Production}kWh price={Price}DKK/kWh amount={Amount}DKK",
                    hour, key.Year, key.Month, totalProduction, price, amount);

                byMonth[key] = (bucket.Amount + amount, bucket.Incomplete);
            }
            else
            {
                _logger.LogDebug(
                    "Settlement calc [total]: hourUtc={HourUtc} year={Year} month={Month} INCOMPLETE hasPrice=false - hour excluded from amount",
                    hour, key.Year, key.Month);

                byMonth[key] = (bucket.Amount, bucket.Incomplete + 1);
            }
        }

        var result = byMonth
            .OrderBy(kv => kv.Key.Year).ThenBy(kv => kv.Key.Month)
            .Select(kv => new MonthlySettlement(kv.Key.Year, kv.Key.Month, Round(kv.Value.Amount), kv.Value.Incomplete))
            .ToList();

        _logger.LogInformation(
            "Total settlement calculated for {Start} to {End}: {MonthCount} month(s), total {Total} DKK",
            localStart, localEnd, result.Count, Round(result.Sum(r => r.Amount)));

        return result;
    }

    private static Dictionary<DateTime, decimal> BucketProductionByHour(IReadOnlyList<MeterReading> readings)
    {
        var byHour = new Dictionary<DateTime, decimal>();
        foreach (var reading in readings)
        {
            var hour = FloorToHour(reading.TimestampUtc);
            var production = (decimal)reading.Production;
            byHour[hour] = byHour.TryGetValue(hour, out var existing) ? existing + production : production;
        }

        return byHour;
    }

    private (DateTime UtcStart, DateTime UtcEndExclusive) ToUtcRange(DateOnly localStart, DateOnly localEnd)
    {
        var localStartDt = DateTime.SpecifyKind(localStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localEndExclusiveDt = DateTime.SpecifyKind(localEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);

        return (
            TimeZoneInfo.ConvertTimeToUtc(localStartDt, _timeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEndExclusiveDt, _timeZone));
    }

    private static DateTime FloorToHour(DateTime dt) => new(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Kind);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            // Defensive fallback for environments without full IANA tzdata
            // rather than crashing app startup.
            return TimeZoneInfo.Utc;
        }
    }
}
