using Assets.Domain.Assets;
using FluentAssertions;
using Xunit;

namespace Assets.Domain.Tests;

public class AssetTests
{
    [Fact]
    public void WindTurbine_AssetType_IsWindTurbine()
    {
        var turbine = new WindTurbine { Capacity = 100, MeterPointId = "123", HubHeight = 80, RotorDiameter = 120 };

        turbine.AssetType.Should().Be("WindTurbine");
    }

    [Fact]
    public void SolarPanel_AssetType_IsSolarPanel()
    {
        var panel = new SolarPanel { Capacity = 50, MeterPointId = "456", CompassOrientation = "South" };

        panel.AssetType.Should().Be("SolarPanel");
    }

    [Fact]
    public void Asset_Id_IsGeneratedAndNotEmpty()
    {
        var turbine = new WindTurbine();

        turbine.Id.Should().NotBeNullOrWhiteSpace();
    }
}
