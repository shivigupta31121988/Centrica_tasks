using System.Reflection;
using System.Text.Json;
using Assets.Api.Dtos;
using Assets.Domain.Assets;

namespace Assets.Api.Mapping;

/// <summary>
/// Reflection-based mapping keyed off [AssetField] attributes, so adding a
/// new asset type never requires touching this class - it already knows
/// how to map any property the domain class declares as an AssetField.
/// </summary>
public static class AssetMapper
{
    public static AssetDto ToDto(Asset asset)
    {
        var dto = new AssetDto { Id = asset.Id, Type = asset.AssetType };

        foreach (var prop in GetFieldProperties(asset.GetType()))
        {
            var camelName = ToCamelCase(prop.Name);
            dto.Fields[camelName] = prop.GetValue(asset);
        }

        return dto;
    }

    /// <summary>
    /// Builds the concrete domain Asset from the request DTO. Returns null
    /// if the requested type isn't registered (caller should return 400).
    /// </summary>
    public static Asset? ToDomain(CreateAssetDto dto)
    {
        if (!AssetTypeRegistry.TryGetType(dto.Type, out var clrType) || clrType is null)
        {
            return null;
        }

        var asset = (Asset)Activator.CreateInstance(clrType)!;

        foreach (var prop in GetFieldProperties(clrType))
        {
            var camelName = ToCamelCase(prop.Name);

            object? value = camelName switch
            {
                "capacity" => dto.Capacity,
                "meterPointId" => dto.MeterPointId,
                _ when dto.Fields.TryGetValue(camelName, out var element) => ConvertJsonElement(element, prop.PropertyType),
                _ => null,
            };

            if (value is not null)
            {
                prop.SetValue(asset, value);
            }
        }

        return asset;
    }

    private static IEnumerable<PropertyInfo> GetFieldProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<AssetFieldAttribute>(inherit: true) is not null)
            .OrderBy(p => p.GetCustomAttribute<AssetFieldAttribute>(inherit: true)!.Order);

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

    private static object ConvertJsonElement(JsonElement element, Type targetType)
    {
        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            return element.GetDouble();
        }

        if (targetType == typeof(string))
        {
            return element.GetString() ?? string.Empty;
        }

        throw new NotSupportedException($"Unsupported asset field type: {targetType}");
    }
}
