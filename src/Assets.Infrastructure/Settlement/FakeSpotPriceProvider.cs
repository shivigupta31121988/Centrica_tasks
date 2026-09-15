using Assets.Domain.Settlement;

namespace Assets.Infrastructure.Settlement;

/// <summary>
/// Generates a plausible hourly DKK/kWh price series. Deterministic - the
/// same hour always yields the same price (seeded from the hour itself),
/// so tests and demos are reproducible rather than flaky. Swap for
/// HttpSpotPriceProvider (calling the real API) via a one-line DI change
/// once that API is available - SettlementCalculator only depends on
/// ISpotPriceProvider, never on which implementation is behind it.
/// </summary>
public sealed class FakeSpotPriceProvider : ISpotPriceProvider
{
    private const decimal MinPrice = 1.5m;
    private const decimal MaxPrice = 4.0m;

    public Task<IReadOnlyList<SpotPriceInterval>> GetPricesAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var intervals = new List<SpotPriceInterval>();

        for (var hour = FloorToHour(startUtc); hour < endUtc; hour = hour.AddHours(1))
        {
            ct.ThrowIfCancellationRequested();
            intervals.Add(new SpotPriceInterval(hour, hour.AddHours(1), PriceForHour(hour)));
        }

        return Task.FromResult<IReadOnlyList<SpotPriceInterval>>(intervals);
    }

    private static decimal PriceForHour(DateTime hourUtc)
    {
        var hoursSinceEpoch = (long)(hourUtc - DateTime.UnixEpoch).TotalHours;
        var rng = new Random(unchecked((int)hoursSinceEpoch));
        var value = MinPrice + (decimal)rng.NextDouble() * (MaxPrice - MinPrice);
        return Math.Round(value, 2);
    }

    private static DateTime FloorToHour(DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0, dt.Kind);
}
