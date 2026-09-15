using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Assets.Domain.Assets;

/// <summary>
/// Base type for every renewable asset in the system.
///
/// NOTE ON ARCHITECTURE: in a stricter clean-architecture setup, Mongo
/// driver attributes would not appear on the domain model at all (the
/// Infrastructure layer would own a separate persistence model and map
/// between the two). For this MVP the two are intentionally the same
/// class to keep the mapping layer thin - the trade-off is documented
/// here rather than hidden.
/// </summary>
[BsonKnownTypes(typeof(WindTurbine), typeof(SolarPanel), typeof(UnclassifiedAsset))]
public abstract class Asset
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>
    /// String discriminator identifying the concrete asset type
    /// (e.g. "WindTurbine"). Also used as the lookup key into the
    /// AssetTypeRegistry and as the Mongo discriminator value.
    /// </summary>
    public abstract string AssetType { get; }

    [AssetField("Capacity (kW)", "number", order: 0)]
    public double Capacity { get; set; }

    [AssetField("Meter Point ID", "text", order: 1)]
    public string MeterPointId { get; set; } = string.Empty;
}
