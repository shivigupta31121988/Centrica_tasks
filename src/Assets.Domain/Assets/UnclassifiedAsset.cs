namespace Assets.Domain.Assets;

/// <summary>
/// Auto-created by the meter data importer when a file's meter point id
/// doesn't match any existing asset (per the brief: "create an asset with
/// meter point id X if it doesn't already exist"). Deliberately has no
/// extra fields beyond the common ones - an Admin is expected to edit it
/// into a real WindTurbine/SolarPanel later via the normal UI.
///
/// This is also a real exercise of the Task 1 extensibility model: adding
/// this type required exactly the two documented steps (this class, plus
/// one registry/class-map entry each) - no changes to the repository,
/// controllers, or import pipeline.
/// </summary>
public sealed class UnclassifiedAsset : Asset
{
    public override string AssetType => "Unclassified";
}
