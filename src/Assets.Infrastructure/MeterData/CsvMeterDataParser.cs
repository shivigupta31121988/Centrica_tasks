using System.Globalization;
using Assets.Domain.MeterData;
using CsvHelper;
using CsvHelper.Configuration;

namespace Assets.Infrastructure.MeterData;

/// <summary>
/// Expects a header row with "Timestamp" and "Production" columns (the
/// two-column shape confirmed for this format). A malformed row (bad
/// date, non-numeric production) is skipped rather than aborting the
/// whole file - noted here since it's a real design choice, not an
/// oversight: one bad row shouldn't block importing the rest of a file.
/// </summary>
public sealed class CsvMeterDataParser : IMeterDataParser
{
    public bool CanParse(string filePath) =>
        Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<MeterReadingRecord>> ParseAsync(string filePath, CancellationToken ct = default)
    {
        var records = new List<MeterReadingRecord>();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var streamReader = new StreamReader(filePath);
        using var csv = new CsvReader(streamReader, config);

        await csv.ReadAsync();
        csv.ReadHeader();

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();

            var timestampRaw = csv.GetField("Timestamp");
            var productionRaw = csv.GetField("Production");

            if (TryParseRow(timestampRaw, productionRaw, out var record))
            {
                records.Add(record);
            }
            // else: malformed row - silently skipped, see class summary.
        }

        return records;
    }

    internal static bool TryParseRow(string? timestampRaw, string? productionRaw, out MeterReadingRecord record)
    {
        record = default!;

        if (!DateTime.TryParse(
                timestampRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var timestamp))
        {
            return false;
        }

        if (!double.TryParse(productionRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var production))
        {
            return false;
        }

        record = new MeterReadingRecord(timestamp, production);
        return true;
    }
}
