using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Assets.Api.Dtos;

/// <summary>
/// Request DTO exposed to API clients - kept separate from the domain
/// model (per the brief's preferred practices). Deliberately generic
/// ("Fields" dictionary) rather than one DTO per asset type, so a new
/// asset type never requires a new DTO class: the frontend sends
/// whatever fields /api/asset-types told it about, keyed by the same
/// camelCase field names, and AssetMapper reflects them onto the
/// right domain property.
/// </summary>
public sealed class CreateAssetDto
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public double Capacity { get; set; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string MeterPointId { get; set; } = string.Empty;

    /// <summary>Type-specific field values, keyed by camelCase property name (e.g. "hubHeight").</summary>
    public Dictionary<string, JsonElement> Fields { get; set; } = new();
}

/// <summary>
/// Response DTO. Flattened so the React table/search can work generically
/// against any asset type without per-type rendering code.
/// </summary>
public sealed class AssetDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object?> Fields { get; set; } = new();
}
