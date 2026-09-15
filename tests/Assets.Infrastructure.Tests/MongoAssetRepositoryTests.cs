using Assets.Domain.Assets;
using Assets.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace Assets.Infrastructure.Tests;

public class MongoAssetRepositoryTests : IClassFixture<MongoFixture>
{
    private readonly MongoAssetRepository _repository;

    public MongoAssetRepositoryTests(MongoFixture fixture)
    {
        _repository = new MongoAssetRepository(fixture.Context);
    }

    [Fact]
    public async Task AddAsync_ThenGetAll_ReturnsThePersistedAsset()
    {
        var turbine = new WindTurbine
        {
            Capacity = 150,
            MeterPointId = $"wt-{Guid.NewGuid()}",
            HubHeight = 90,
            RotorDiameter = 130,
        };

        await _repository.AddAsync(turbine);
        var all = await _repository.GetAllAsync();

        all.Should().ContainSingle(a => a.MeterPointId == turbine.MeterPointId)
            .Which.Should().BeOfType<WindTurbine>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsBothAssetTypes_CorrectlyDeserialized()
    {
        var meterPointId = $"sp-{Guid.NewGuid()}";
        var panel = new SolarPanel
        {
            Capacity = 20,
            MeterPointId = meterPointId,
            CompassOrientation = "South-West",
        };

        await _repository.AddAsync(panel);
        var all = await _repository.GetAllAsync();

        var found = all.Should().ContainSingle(a => a.MeterPointId == meterPointId).Subject;
        found.Should().BeOfType<SolarPanel>();
        ((SolarPanel)found).CompassOrientation.Should().Be("South-West");
    }

    [Fact]
    public async Task GetByMeterPointIdAsync_WhenNoMatch_ReturnsNull()
    {
        var result = await _repository.GetByMeterPointIdAsync($"does-not-exist-{Guid.NewGuid()}");

        result.Should().BeNull();
    }
}
