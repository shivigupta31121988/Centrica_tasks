using Assets.Domain.Assets;
using Assets.Infrastructure.Settlement;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Assets.Infrastructure.Tests;

public class SettlementCalculatorTests
{
    private static SettlementCalculator CreateCalculator(
        FakeAssetRepository assets, FakeMeterReadingRepository readings, FakeFixedSpotPriceProvider prices)
    {
        var settings = Options.Create(new SettlementSettings());
        return new SettlementCalculator(assets, readings, prices, settings, NullLogger<SettlementCalculator>.Instance);
    }

    [Fact]
    public async Task CalculateForAssetAsync_UnknownAssetId_ReturnsNull()
    {
        var calculator = CreateCalculator(new FakeAssetRepository(), new FakeMeterReadingRepository(), new FakeFixedSpotPriceProvider());

        var result = await calculator.CalculateForAssetAsync("missing-id", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 1));

        result.Should().BeNull();
    }

    [Fact]
    public async Task CalculateForAssetAsync_BasicArithmetic_ProductionTimesPrice()
    {
        var assets = new FakeAssetRepository();
        var asset = new WindTurbine { Id = "a1", MeterPointId = "mp1", Capacity = 100 };
        assets.Add(asset);

        var readings = new FakeMeterReadingRepository();
        // Local Copenhagen midday in January (CET, UTC+1) -> 11:00 UTC.
        var hourUtc = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
        readings.Add("mp1", hourUtc, production: 10.0);

        var prices = new FakeFixedSpotPriceProvider();
        prices.SetPrice(hourUtc, 2.5m);

        var calculator = CreateCalculator(assets, readings, prices);

        var result = await calculator.CalculateForAssetAsync("a1", new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 15));

        result.Should().ContainSingle();
        result![0].Amount.Should().Be(25.0m); // 10 kWh * 2.5 DKK/kWh
        result[0].IncompleteHours.Should().Be(23); // the other 23 hours of the day have neither reading nor price
    }

    [Fact]
    public async Task CalculateForAssetAsync_MissingPriceForAnHour_ExcludesItAndFlagsIncomplete()
    {
        var assets = new FakeAssetRepository();
        assets.Add(new WindTurbine { Id = "a1", MeterPointId = "mp1", Capacity = 100 });

        var readings = new FakeMeterReadingRepository();
        var hour1 = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var hour2 = new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc);
        readings.Add("mp1", hour1, 10.0);
        readings.Add("mp1", hour2, 20.0); // no price will be set for this hour

        var prices = new FakeFixedSpotPriceProvider();
        prices.SetPrice(hour1, 2.0m);
        // hour2 deliberately has no price.

        var calculator = CreateCalculator(assets, readings, prices);
        var result = await calculator.CalculateForAssetAsync("a1", new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 15));

        result![0].Amount.Should().Be(20.0m); // only hour1's 10 * 2.0 contributes
        result[0].IncompleteHours.Should().Be(23); // hour2 (missing price) + 22 hours with neither
    }

    [Fact]
    public async Task CalculateForAssetAsync_RoundsUsingBankersRounding()
    {
        var assets = new FakeAssetRepository();
        assets.Add(new WindTurbine { Id = "a1", MeterPointId = "mp1", Capacity = 100 });

        var readings = new FakeMeterReadingRepository();
        var hour = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        // production * price is engineered to land exactly on a rounding
        // midpoint: 1.005 -> banker's rounding takes it to 1.00 (even),
        // whereas away-from-zero rounding would give 1.01.
        readings.Add("mp1", hour, 1.0);

        var prices = new FakeFixedSpotPriceProvider();
        prices.SetPrice(hour, 1.005m);

        var calculator = CreateCalculator(assets, readings, prices);
        var result = await calculator.CalculateForAssetAsync("a1", new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 15));

        result![0].Amount.Should().Be(1.00m);
    }

    [Fact]
    public async Task CalculateForAssetAsync_LocalTimeBucketing_UtcLateEveningFallsIntoNextLocalDay()
    {
        var assets = new FakeAssetRepository();
        assets.Add(new WindTurbine { Id = "a1", MeterPointId = "mp1", Capacity = 100 });

        var readings = new FakeMeterReadingRepository();
        // 23:00 UTC on Jan 1st = 00:00 CET on Jan 2nd (Copenhagen is UTC+1 in January).
        var hourUtc = new DateTime(2024, 1, 1, 23, 0, 0, DateTimeKind.Utc);
        readings.Add("mp1", hourUtc, 5.0);

        var prices = new FakeFixedSpotPriceProvider();
        prices.SetPrice(hourUtc, 2.0m);

        var calculator = CreateCalculator(assets, readings, prices);
        // Query a range spanning both local days so the bucketing choice is visible.
        var result = await calculator.CalculateForAssetAsync("a1", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 2));

        var jan1 = result!.Single(r => r.LocalDate == new DateOnly(2024, 1, 1));
        var jan2 = result.Single(r => r.LocalDate == new DateOnly(2024, 1, 2));

        jan1.Amount.Should().Be(0m, "the reading belongs to the local Jan 2nd day, not Jan 1st UTC-wise");
        jan2.Amount.Should().Be(10.0m);
    }

    [Fact]
    public async Task CalculateTotalAsync_SumsAcrossAllAssetsSharingAnHour_BucketedByLocalMonth()
    {
        var assets = new FakeAssetRepository();
        assets.Add(new WindTurbine { Id = "a1", MeterPointId = "mp1", Capacity = 100 });
        assets.Add(new SolarPanel { Id = "a2", MeterPointId = "mp2", Capacity = 50 });

        var readings = new FakeMeterReadingRepository();
        var hour = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        readings.Add("mp1", hour, 10.0);
        readings.Add("mp2", hour, 5.0);

        var prices = new FakeFixedSpotPriceProvider();
        prices.SetPrice(hour, 2.0m);

        var calculator = CreateCalculator(assets, readings, prices);
        var result = await calculator.CalculateTotalAsync(new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 15));

        result.Should().ContainSingle();
        result[0].Amount.Should().Be(30.0m); // (10 + 5) kWh * 2.0 DKK/kWh
        result[0].Year.Should().Be(2024);
        result[0].Month.Should().Be(1);
    }
}
