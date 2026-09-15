using System.Text.Json;
using Assets.Api.Dtos;
using Assets.Api.Mapping;
using Assets.Domain.Assets;
using FluentAssertions;
using Xunit;

namespace Assets.Api.Tests;

public class AssetMapperTests
{
    [Fact]
    public void ToDomain_UnknownType_ReturnsNull()
    {
        var dto = new CreateAssetDto { Type = "Battery", Capacity = 10, MeterPointId = "b-1" };

        AssetMapper.ToDomain(dto).Should().BeNull();
    }

    [Fact]
    public void ToDomain_WindTurbine_MapsAllFields()
    {
        var dto = new CreateAssetDto { Type = "WindTurbine", Capacity = 150, MeterPointId = "wt-1" };
        dto.Fields["hubHeight"] = JsonSerializer.SerializeToElement(85.5);
        dto.Fields["rotorDiameter"] = JsonSerializer.SerializeToElement(125.0);

        var asset = AssetMapper.ToDomain(dto);

        asset.Should().BeOfType<WindTurbine>();
        var turbine = (WindTurbine)asset!;
        turbine.Capacity.Should().Be(150);
        turbine.MeterPointId.Should().Be("wt-1");
        turbine.HubHeight.Should().Be(85.5);
        turbine.RotorDiameter.Should().Be(125.0);
    }

    [Fact]
    public void ToDto_SolarPanel_FlattensAllFieldsWithCamelCaseKeys()
    {
        var panel = new SolarPanel { Capacity = 30, MeterPointId = "sp-9", CompassOrientation = "North-East" };

        var dto = AssetMapper.ToDto(panel);

        dto.Type.Should().Be("SolarPanel");
        dto.Fields["capacity"].Should().Be(30.0);
        dto.Fields["meterPointId"].Should().Be("sp-9");
        dto.Fields["compassOrientation"].Should().Be("North-East");
    }

    [Fact]
    public void RoundTrip_ToDomainThenToDto_PreservesValues()
    {
        var dto = new CreateAssetDto { Type = "SolarPanel", Capacity = 42, MeterPointId = "sp-round-trip" };
        dto.Fields["compassOrientation"] = JsonSerializer.SerializeToElement("South-West");

        var domain = AssetMapper.ToDomain(dto)!;
        var responseDto = AssetMapper.ToDto(domain);

        responseDto.Fields["capacity"].Should().Be(42.0);
        responseDto.Fields["meterPointId"].Should().Be("sp-round-trip");
        responseDto.Fields["compassOrientation"].Should().Be("South-West");
    }
}
