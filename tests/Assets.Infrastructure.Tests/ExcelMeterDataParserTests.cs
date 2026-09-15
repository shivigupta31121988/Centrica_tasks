using Assets.Infrastructure.MeterData;
using ClosedXML.Excel;
using FluentAssertions;
using Xunit;

namespace Assets.Infrastructure.Tests;

public class ExcelMeterDataParserTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory().FullName;
    private readonly ExcelMeterDataParser _parser = new();

    [Fact]
    public void CanParse_OnlyAcceptsXlsxExtension()
    {
        _parser.CanParse("123.xlsx").Should().BeTrue();
        _parser.CanParse("123.csv").Should().BeFalse();
    }

    [Fact]
    public async Task ParseAsync_WellFormedWorkbook_ReturnsAllRows()
    {
        var path = Path.Combine(_tempDir, "readings.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "Timestamp";
            sheet.Cell(1, 2).Value = "Production";
            sheet.Cell(2, 1).Value = "2023-11-21T16:00:00Z";
            sheet.Cell(2, 2).Value = 12.5;
            sheet.Cell(3, 1).Value = "2023-11-21T17:00:00Z";
            sheet.Cell(3, 2).Value = 13.1;
            workbook.SaveAs(path);
        }

        var records = await _parser.ParseAsync(path);

        records.Should().HaveCount(2);
        records[0].Production.Should().Be(12.5);
        records[1].Production.Should().Be(13.1);
    }

    [Fact]
    public async Task ParseAsync_ColumnsInDifferentOrder_StillFindsThemByHeaderName()
    {
        var path = Path.Combine(_tempDir, "reordered.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "Production";
            sheet.Cell(1, 2).Value = "Timestamp";
            sheet.Cell(2, 1).Value = 20.0;
            sheet.Cell(2, 2).Value = "2023-11-21T16:00:00Z";
            workbook.SaveAs(path);
        }

        var records = await _parser.ParseAsync(path);

        records.Should().ContainSingle();
        records[0].Production.Should().Be(20.0);
    }

    [Fact]
    public async Task ParseAsync_MissingExpectedHeaders_ReturnsEmptyRatherThanThrowing()
    {
        var path = Path.Combine(_tempDir, "wrong-headers.xlsx");
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Sheet1");
            sheet.Cell(1, 1).Value = "SomethingElse";
            workbook.SaveAs(path);
        }

        var records = await _parser.ParseAsync(path);

        records.Should().BeEmpty();
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
