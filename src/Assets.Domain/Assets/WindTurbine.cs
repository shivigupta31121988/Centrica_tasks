namespace Assets.Domain.Assets;

public sealed class WindTurbine : Asset
{
    public override string AssetType => "WindTurbine";

    [AssetField("Hub Height (m)", "number", order: 2)]
    public double HubHeight { get; set; }

    [AssetField("Rotor Diameter (m)", "number", order: 3)]
    public double RotorDiameter { get; set; }
}
