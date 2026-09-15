using System.Reflection;

namespace Assets.Domain.Assets;

public sealed class AssetFieldMetadata
{
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string InputType { get; init; } = string.Empty;
}

public sealed class AssetTypeMetadata
{
    public string Type { get; init; } = string.Empty;
    public List<AssetFieldMetadata> Fields { get; init; } = new();
}

/// <summary>
/// The single place to register a new renewable asset type.
///
/// To add a new asset type in the future:
///   1. Create the domain class (inherit Asset, add [AssetField] properties).
///   2. Add [BsonKnownTypes] entry on Asset for it (Mongo (de)serialization).
///   3. Add one line to the dictionary below.
///
/// Nothing else in the system needs to change: the repository, the list
/// endpoint, and the React UI all derive their behaviour from this
/// registry (the UI via reflection exposed through /api/asset-types).
/// </summary>
public static class AssetTypeRegistry
{
    private static readonly Dictionary<string, Type> Types = new()
    {
        ["WindTurbine"] = typeof(WindTurbine),
        ["SolarPanel"] = typeof(SolarPanel),
        ["Unclassified"] = typeof(UnclassifiedAsset),
    };

    public static IReadOnlyDictionary<string, Type> All => Types;

    public static bool TryGetType(string assetType, out Type? type) =>
        Types.TryGetValue(assetType, out type);

    /// <summary>
    /// Builds field metadata for every registered asset type by reflecting
    /// over [AssetField] attributes. Drives the schema-driven create form.
    /// </summary>
    public static IReadOnlyList<AssetTypeMetadata> BuildMetadata()
    {
        var result = new List<AssetTypeMetadata>();

        foreach (var (typeName, clrType) in Types)
        {
            var fields = clrType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new
                {
                    Property = p,
                    Attr = p.GetCustomAttribute<AssetFieldAttribute>(inherit: true),
                })
                .Where(x => x.Attr is not null)
                .OrderBy(x => x.Attr!.Order)
                .Select(x => new AssetFieldMetadata
                {
                    Name = ToCamelCase(x.Property.Name),
                    Label = x.Attr!.Label,
                    InputType = x.Attr.InputType,
                })
                .ToList();

            result.Add(new AssetTypeMetadata { Type = typeName, Fields = fields });
        }

        return result;
    }

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
