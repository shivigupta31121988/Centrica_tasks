using Assets.Infrastructure.Settlement;
using FluentAssertions;
using Xunit;

namespace Assets.Infrastructure.Tests;

public class FakeSpotPriceProviderTests
{
    private readonly FakeSpotPriceProvider _provider = new();

    [Fact]
    public async Task GetPricesAsync_SameRangeTwice_ReturnsIdenticalPrices()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(24);

        var first = await _provider.GetPricesAsync(start, end);
        var second = await _provider.GetPricesAsync(start, end);

        first.Should().BeEquivalentTo(second, "the same hour must always produce the same price - tests and demos must be reproducible");
    }

    [Fact]
    public async Task GetPricesAsync_AllPricesWithinDocumentedBand()
    {
        var start = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(48);

        var prices = await _provider.GetPricesAsync(start, end);

        prices.Should().OnlyContain(p => p.PricePerKwhDkk >= 1.5m && p.PricePerKwhDkk <= 4.0m);
    }

    [Fact]
    public async Task GetPricesAsync_ReturnsOneIntervalPerHour()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(6);

        var prices = await _provider.GetPricesAsync(start, end);

        prices.Should().HaveCount(6);
    }
}
