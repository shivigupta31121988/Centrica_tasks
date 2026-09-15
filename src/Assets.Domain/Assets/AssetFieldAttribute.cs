namespace Assets.Domain.Assets;

/// <summary>
/// Marks a property as a user-facing field. Reflected over by the API's
/// /api/asset-types endpoint to drive a schema-driven React form, so that
/// adding a new asset type never requires a frontend code change.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AssetFieldAttribute : Attribute
{
    public string Label { get; }

    /// <summary>
    /// A simple input-type hint for the frontend: "number" or "text".
    /// Kept intentionally small for MVP - extend as new field kinds are needed.
    /// </summary>
    public string InputType { get; }

    /// <summary>
    /// Order fields should appear in the generated form. Lower first.
    /// </summary>
    public int Order { get; }

    public AssetFieldAttribute(string label, string inputType, int order = 0)
    {
        Label = label;
        InputType = inputType;
        Order = order;
    }
}
