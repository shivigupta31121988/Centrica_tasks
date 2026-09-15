using System.Text.Json;
using Assets.Api.Controllers;
using Assets.Api.Dtos;
using Assets.Domain.Assets;
using Assets.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Assets.Api.Tests;

public class AssetsControllerTests
{
    private readonly Mock<IAssetRepository> _repository = new();
    private readonly AssetsController _controller;

    public AssetsControllerTests()
    {
        _controller = new AssetsController(_repository.Object, Mock.Of<ILogger<AssetsController>>());
    }

    [Fact]
    public async Task GetAll_ReturnsMappedDtos_ForMixedAssetTypes()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Asset>
            {
                new WindTurbine { Capacity = 100, MeterPointId = "wt-1", HubHeight = 80, RotorDiameter = 120 },
                new SolarPanel { Capacity = 20, MeterPointId = "sp-1", CompassOrientation = "South" },
            });

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        var dtos = ok!.Value as List<AssetDto>;
        dtos.Should().HaveCount(2);
        dtos.Should().Contain(d => d.Type == "WindTurbine" && (string)d.Fields["meterPointId"]! == "wt-1");
        dtos.Should().Contain(d => d.Type == "SolarPanel" && (string)d.Fields["meterPointId"]! == "sp-1");
    }

    [Fact]
    public async Task Create_WithUnknownType_ReturnsBadRequest()
    {
        var request = new CreateAssetDto { Type = "Battery", Capacity = 10, MeterPointId = "b-1" };

        var result = await _controller.Create(request, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _repository.Verify(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithDuplicateMeterPointId_ReturnsConflict()
    {
        var request = new CreateAssetDto { Type = "SolarPanel", Capacity = 10, MeterPointId = "sp-1" };
        request.Fields["compassOrientation"] = JsonSerializer.SerializeToElement("South");

        _repository.Setup(r => r.GetByMeterPointIdAsync("sp-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SolarPanel { MeterPointId = "sp-1" });

        var result = await _controller.Create(request, CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
        _repository.Verify(r => r.AddAsync(It.IsAny<Asset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithValidWindTurbineRequest_PersistsAndReturnsDto()
    {
        var request = new CreateAssetDto { Type = "WindTurbine", Capacity = 150, MeterPointId = "wt-42" };
        request.Fields["hubHeight"] = JsonSerializer.SerializeToElement(90.0);
        request.Fields["rotorDiameter"] = JsonSerializer.SerializeToElement(130.0);

        _repository.Setup(r => r.GetByMeterPointIdAsync("wt-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Asset?)null);

        var result = await _controller.Create(request, CancellationToken.None);

        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _repository.Verify(
            r => r.AddAsync(It.Is<Asset>(a => a is WindTurbine && ((WindTurbine)a).HubHeight == 90 && ((WindTurbine)a).RotorDiameter == 130), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
