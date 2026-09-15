using Assets.Domain.Assets;
using Assets.Domain.MeterData;
using Assets.Domain.Settlement;
using Assets.Infrastructure.MeterData;
using Assets.Infrastructure.Persistence;

namespace Assets.Infrastructure.Tests;

/// <summary>Fake asset repository - only GetByIdAsync is used by SettlementCalculator.</summary>
public sealed class FakeAssetRepository : IAssetRepository
{
    private readonly Dictionary<string, Asset> _assets = new();

    public void Add(Asset asset) => _assets[asset.Id] = asset;

    public Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Asset>>(_assets.Values.ToList());

    public Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_assets.GetValueOrDefault(id));

    public Task<Asset?> GetByMeterPointIdAsync(string meterPointId, CancellationToken ct = default) =>
        Task.FromResult(_assets.Values.FirstOrDefault(a => a.MeterPointId == meterPointId));

    public Task AddAsync(Asset asset, CancellationToken ct = default)
    {
        Add(asset);
        return Task.CompletedTask;
    }
}

/// <summary>Fake meter reading repository - in-memory list, range-filtered like the real one.</summary>
public sealed class FakeMeterReadingRepository : IMeterReadingRepository
{
    private readonly List<MeterReading> _readings = new();

    public void Add(string meterPointId, DateTime timestampUtc, double production) =>
        _readings.Add(new MeterReading { MeterPointId = meterPointId, TimestampUtc = timestampUtc, Production = production });

    public Task<(int Imported, int Skipped)> UpsertManyAsync(IReadOnlyList<MeterReading> readings, CancellationToken ct = default)
    {
        _readings.AddRange(readings);
        return Task.FromResult((readings.Count, 0));
    }

    public Task<IReadOnlyList<MeterReading>> GetByMeterPointIdInRangeAsync(string meterPointId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MeterReading>>(
            _readings.Where(r => r.MeterPointId == meterPointId && r.TimestampUtc >= startUtc && r.TimestampUtc < endUtc).ToList());

    public Task<IReadOnlyList<MeterReading>> GetAllInRangeAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<MeterReading>>(
            _readings.Where(r => r.TimestampUtc >= startUtc && r.TimestampUtc < endUtc).ToList());
}

/// <summary>Fake spot price provider - fixed price per hour, settable per test, missing hours simulate "no price available".</summary>
public sealed class FakeFixedSpotPriceProvider : ISpotPriceProvider
{
    private readonly Dictionary<DateTime, decimal> _prices = new();

    public void SetPrice(DateTime hourUtc, decimal pricePerKwhDkk) => _prices[hourUtc] = pricePerKwhDkk;

    public Task<IReadOnlyList<SpotPriceInterval>> GetPricesAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var intervals = _prices
            .Where(kv => kv.Key >= startUtc && kv.Key < endUtc)
            .Select(kv => new SpotPriceInterval(kv.Key, kv.Key.AddHours(1), kv.Value))
            .ToList();

        return Task.FromResult<IReadOnlyList<SpotPriceInterval>>(intervals);
    }
}
