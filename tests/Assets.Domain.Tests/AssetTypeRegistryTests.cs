using Assets.Domain.Assets;
using FluentAssertions;
using Xunit;

namespace Assets.Domain.Tests;

public class AssetTypeRegistryTests
{
    [Fact]
    public void BuildMetadata_IncludesBothRegisteredAssetTypes()
    {
        var metadata = AssetTypeRegistry.BuildMetadata();

        metadata.Select(m => m.Type).Should().Contain(new[] { "WindTurbine", "SolarPanel" });
    }

    [Fact]
    public void BuildMetadata_WindTurbine_IncludesCommonAndSpecificFields()
    {
        var metadata = AssetTypeRegistry.BuildMetadata()
            .Single(m => m.Type == "WindTurbine");

        var fieldNames = metadata.Fields.Select(f => f.Name).ToList();

        fieldNames.Should().Contain(new[] { "capacity", "meterPointId", "hubHeight", "rotorDiameter" });
    }

    [Fact]
    public void BuildMetadata_SolarPanel_IncludesCommonAndSpecificFields()
    {
        var metadata = AssetTypeRegistry.BuildMetadata()
            .Single(m => m.Type == "SolarPanel");

        var fieldNames = metadata.Fields.Select(f => f.Name).ToList();

        fieldNames.Should().Contain(new[] { "capacity", "meterPointId", "compassOrientation" });
    }

    [Theory]
    [InlineData("WindTurbine", true)]
    [InlineData("SolarPanel", true)]
    [InlineData("Battery", false)]
    [InlineData("", false)]
    public void TryGetType_ReturnsExpectedResult(string type, bool expectedFound)
    {
        var found = AssetTypeRegistry.TryGetType(type, out var clrType);

        found.Should().Be(expectedFound);
        if (expectedFound)
        {
            clrType.Should().NotBeNull();
        }
    }
}
