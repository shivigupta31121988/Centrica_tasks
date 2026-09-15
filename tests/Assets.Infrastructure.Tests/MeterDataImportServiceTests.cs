using Assets.Domain.Assets;
using Assets.Infrastructure.MeterData;
using Assets.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Assets.Infrastructure.Tests;

public class MeterDataImportServiceTests : IClassFixture<MongoFixture>, IDisposable
{
    private readonly MongoFixture _fixture;
    private readonly string _tempDir = Directory.CreateTempSubdirectory().FullName;
    private readonly MeterDataImportService _service;
    private readonly IAssetRepository _assetRepository;

    public MeterDataImportServiceTests(MongoFixture fixture)
    {
        _fixture = fixture;
        _assetRepository = new MongoAssetRepository(fixture.Context);
        var meterReadingRepository = new MongoMeterReadingRepository(fixture.Context);

        var settings = Options.Create(new MeterDataSettings { Directory = _tempDir });

        _service = new MeterDataImportService(
            new IMeterDataParser[] { new CsvMeterDataParser(), new ExcelMeterDataParser() },
            _assetRepository,
            meterReadingRepository,
            settings,
            NullLogger<MeterDataImportService>.Instance);
    }

    [Fact]
    public async Task ImportFileAsync_UnknownMeterPointId_CreatesUnclassifiedAsset()
    {
        var meterPointId = $"new-{Guid.NewGuid()}";
        var filePath = WriteCsv(meterPointId, "2024-03-01T00:00:00Z,10.0\n2024-03-01T01:00:00Z,11.0\n");

        var result = await _service.ImportFileAsync(filePath, meterPointId);

        result.AssetWasCreated.Should().BeTrue();
        result.RowsImported.Should().Be(2);

        var asset = await _assetRepository.GetByMeterPointIdAsync(meterPointId);
        asset.Should().BeOfType<UnclassifiedAsset>();
    }

    [Fact]
    public async Task ImportFileAsync_ExistingMeterPointId_DoesNotCreateAnotherAsset()
    {
        var meterPointId = $"existing-{Guid.NewGuid()}";
        await _assetRepository.AddAsync(new WindTurbine { MeterPointId = meterPointId, Capacity = 100 });

        var filePath = WriteCsv(meterPointId, "2024-03-01T00:00:00Z,10.0\n");

        var result = await _service.ImportFileAsync(filePath, meterPointId);

        result.AssetWasCreated.Should().BeFalse();
        var asset = await _assetRepository.GetByMeterPointIdAsync(meterPointId);
        asset.Should().BeOfType<WindTurbine>();
    }

    [Fact]
    public async Task ImportAllInDirectoryAsync_IgnoresFilesNotMatchingNamingConvention()
    {
        File.WriteAllText(Path.Combine(_tempDir, "not-a-meter-point.csv"), "Timestamp,Production\n2024-01-01T00:00:00Z,1\n");
        File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "ignore me");

        var summary = await _service.ImportAllInDirectoryAsync();

        summary.FilesProcessed.Should().Be(0);
    }

    private string WriteCsv(string meterPointId, string dataRows)
    {
        var path = Path.Combine(_tempDir, $"{meterPointId}.csv");
        File.WriteAllText(path, "Timestamp,Production\n" + dataRows);
        return path;
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
