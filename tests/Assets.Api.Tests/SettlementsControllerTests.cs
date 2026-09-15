using Assets.Api.Controllers;
using Assets.Api.Dtos;
using Assets.Domain.Assets;
using Assets.Domain.MeterData;
using Assets.Domain.Settlement;
using Assets.Infrastructure.MeterData;
using Assets.Infrastructure.Persistence;
using Assets.Infrastructure.Settlement;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Assets.Api.Tests;

public class SettlementsControllerTests
{
    private readonly Mock<IAssetRepository> _assetRepository = new();
    private readonly Mock<IMeterReadingRepository> _readingRepository = new();
    private readonly Mock<ISpotPriceProvider> _priceProvider = new();
    private readonly SettlementsController _controller;

    public SettlementsControllerTests()
    {
        var settlementSettings = Options.Create(new SettlementSettings { MaxDailyRouteDays = 366, MaxMonthlyRouteDays = 1100 });
        var calculator = new SettlementCalculator(
            _assetRepository.Object, _readingRepository.Object, _priceProvider.Object,
            settlementSettings, NullLogger<SettlementCalculator>.Instance);

        _controller = new SettlementsController(calculator, settlementSettings);
    }

    [Fact]
    public async Task GetForAsset_UnknownAssetId_ReturnsNotFound()
    {
        _assetRepository.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Asset?)null);

        var result = await _controller.GetForAsset("missing", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 1), CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetForAsset_EndBeforeStart_ReturnsBadRequest()
    {
        var result = await _controller.GetForAsset("a1", new DateOnly(2024, 1, 10), new DateOnly(2024, 1, 1), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _assetRepository.Verify(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetForAsset_RangeExceedsMax_ReturnsBadRequest()
    {
        var result = await _controller.GetForAsset("a1", new DateOnly(2020, 1, 1), new DateOnly(2024, 1, 1), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetForAsset_ValidRequest_ReturnsMappedDailyDtos()
    {
        var asset = new WindTurbine { Id = "a1", MeterPointId = "mp1", Capacity = 100 };
        _assetRepository.Setup(r => r.GetByIdAsync("a1", It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        var hourUtc = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        _readingRepository
            .Setup(r => r.GetByMeterPointIdInRangeAsync("mp1", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeterReading> { new() { MeterPointId = "mp1", TimestampUtc = hourUtc, Production = 10 } });
        _priceProvider
            .Setup(p => p.GetPricesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpotPriceInterval> { new(hourUtc, hourUtc.AddHours(1), 2.0m) });

        var result = await _controller.GetForAsset("a1", new DateOnly(2024, 1, 15), new DateOnly(2024, 1, 15), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var dtos = ok!.Value as List<DailySettlementDto>;
        dtos.Should().ContainSingle();
        dtos![0].Amount.Should().Be(20.0m);
        dtos[0].Currency.Should().Be("DKK");
    }

    [Fact]
    public async Task GetTotal_ValidRequest_ReturnsMappedMonthlyDtos()
    {
        _assetRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Asset>());
        _readingRepository
            .Setup(r => r.GetAllInRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MeterReading>());
        _priceProvider
            .Setup(p => p.GetPricesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpotPriceInterval>());

        var result = await _controller.GetTotal(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 31), CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var dtos = ok!.Value as List<MonthlySettlementDto>;
        dtos.Should().ContainSingle(d => d.Month == "2024-01");
    }
}
