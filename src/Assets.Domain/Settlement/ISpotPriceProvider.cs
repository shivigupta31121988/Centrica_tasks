namespace Assets.Domain.Settlement;

public sealed record SpotPriceInterval(DateTime StartUtc, DateTime EndUtc, decimal PricePerKwhDkk);

/// <summary>
/// Abstraction over the (not-yet-available) real Spot Price REST API.
/// FakeSpotPriceProvider (Infrastructure) is the dev/test substitute
/// described in the brief; a real HttpSpotPriceProvider implementing the
/// same interface can be swapped in later via a one-line DI change -
/// nothing in SettlementCalculator touches this distinction.
/// </summary>
public interface ISpotPriceProvider
{
    Task<IReadOnlyList<SpotPriceInterval>> GetPricesAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
}
