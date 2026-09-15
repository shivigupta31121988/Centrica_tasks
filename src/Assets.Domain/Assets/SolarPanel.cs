namespace Assets.Domain.Assets;

public sealed class SolarPanel : Asset
{
    public override string AssetType => "SolarPanel";

    [AssetField("Compass Orientation", "text", order: 2)]
    public string CompassOrientation { get; set; } = string.Empty;
}
