using Assets.Infrastructure.MeterData;
using FluentAssertions;
using Xunit;

namespace Assets.Infrastructure.Tests;

public class CsvMeterDataParserTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory().FullName;
    private readonly CsvMeterDataParser _parser = new();

    [Fact]
    public void CanParse_OnlyAcceptsCsvExtension()
    {
        _parser.CanParse("123.csv").Should().BeTrue();
        _parser.CanParse("123.xlsx").Should().BeFalse();
        _parser.CanParse("123.CSV").Should().BeTrue();
    }

    [Fact]
    public async Task ParseAsync_WellFormedFile_ReturnsAllRows()
    {
        var path = WriteFile("readings.csv",
            "Timestamp,Production\n" +
            "2023-11-21T16:00:00Z,12.5\n" +
            "2023-11-21T17:00:00Z,13.1\n");

        var records = await _parser.ParseAsync(path);

        records.Should().HaveCount(2);
        records[0].Production.Should().Be(12.5);
        records[1].Production.Should().Be(13.1);
    }

    [Fact]
    public async Task ParseAsync_SkipsMalformedRows_WithoutThrowing()
    {
        var path = WriteFile("readings.csv",
            "Timestamp,Production\n" +
            "2023-11-21T16:00:00Z,12.5\n" +
            "not-a-date,13.1\n" +
            "2023-11-21T18:00:00Z,not-a-number\n" +
            "2023-11-21T19:00:00Z,14.0\n");

        var records = await _parser.ParseAsync(path);

        records.Should().HaveCount(2);
        records.Should().Contain(r => r.Production == 12.5);
        records.Should().Contain(r => r.Production == 14.0);
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_ReturnsEmptyList()
    {
        var path = WriteFile("empty.csv", "Timestamp,Production\n");

        var records = await _parser.ParseAsync(path);

        records.Should().BeEmpty();
    }

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
