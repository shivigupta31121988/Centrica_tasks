using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Assets.Domain.MeterData;

public sealed class ImportJob
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string FileName { get; set; } = string.Empty;

    public string MeterPointId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;

    /// <summary>Set when a worker actually picks the job up (not when it was queued).</summary>
    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int RowsImported { get; set; }

    public int RowsSkipped { get; set; }

    public string? ErrorMessage { get; set; }

    public string UploadedBy { get; set; } = string.Empty;

    /// <summary>
    /// Elapsed time is deliberately never stored - it's always computed
    /// fresh against a clock, so a still-Processing job reports a
    /// correctly ticking duration on every poll with no push mechanism
    /// needed. Pure function of the entity's own state - easy to unit
    /// test without a real clock or a real delay.
    /// </summary>
    public double GetElapsedSeconds(DateTime nowUtc)
    {
        if (StartedAtUtc is null)
        {
            return 0;
        }

        var end = CompletedAtUtc ?? nowUtc;
        return Math.Max(0, (end - StartedAtUtc.Value).TotalSeconds);
    }
}
