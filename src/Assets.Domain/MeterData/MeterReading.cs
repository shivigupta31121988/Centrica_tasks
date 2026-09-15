using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Assets.Domain.MeterData;

public sealed class MeterReading
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string MeterPointId { get; set; } = string.Empty;

    public DateTime TimestampUtc { get; set; }

    public double Production { get; set; }

    /// <summary>Filename it was imported from - kept for traceability/debugging.</summary>
    public string SourceFile { get; set; } = string.Empty;

    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;
}
