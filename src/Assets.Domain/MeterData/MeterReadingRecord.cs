namespace Assets.Domain.MeterData;

/// <summary>
/// What every IMeterDataParser produces, regardless of source format.
/// Deliberately just two fields - matches the (timestamp, production)
/// shape confirmed for both the CSV and Excel formats.
/// </summary>
public sealed record MeterReadingRecord(DateTime TimestampUtc, double Production);
