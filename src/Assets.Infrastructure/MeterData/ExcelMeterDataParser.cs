using Assets.Domain.MeterData;
using ClosedXML.Excel;

namespace Assets.Infrastructure.MeterData;

/// <summary>
/// Expects the same two-column shape as the CSV format ("Timestamp",
/// "Production" headers), just on the first worksheet of an .xlsx file.
/// Reads cell VALUES only - workbook formulas are never evaluated, which
/// closes off a class of injection/side-effect risk from an uploaded file.
/// </summary>
public sealed class ExcelMeterDataParser : IMeterDataParser
{
    public bool CanParse(string filePath) =>
        Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<MeterReadingRecord>> ParseAsync(string filePath, CancellationToken ct = default)
    {
        var records = new List<MeterReadingRecord>();

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.Row(1);

        var timestampColumn = FindColumn(headerRow, "Timestamp");
        var productionColumn = FindColumn(headerRow, "Production");

        if (timestampColumn is null || productionColumn is null)
        {
            // No recognisable header - treat as zero readings rather than
            // throwing, consistent with the CSV parser's "skip bad rows"
            // philosophy applied at the whole-file level.
            return Task.FromResult<IReadOnlyList<MeterReadingRecord>>(records);
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            ct.ThrowIfCancellationRequested();

            var row = worksheet.Row(rowNumber);
            var timestampRaw = row.Cell(timestampColumn.Value).GetString();
            var productionRaw = row.Cell(productionColumn.Value).GetString();

            if (CsvMeterDataParser.TryParseRow(timestampRaw, productionRaw, out var record))
            {
                records.Add(record);
            }
        }

        return Task.FromResult<IReadOnlyList<MeterReadingRecord>>(records);
    }

    private static int? FindColumn(IXLRow headerRow, string headerName)
    {
        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var col = 1; col <= lastColumn; col++)
        {
            if (string.Equals(headerRow.Cell(col).GetString(), headerName, StringComparison.OrdinalIgnoreCase))
            {
                return col;
            }
        }

        return null;
    }
}
