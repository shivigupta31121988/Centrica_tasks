using Assets.Domain.Assets;
using Assets.Domain.MeterData;
using Assets.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Assets.Infrastructure.MeterData;

public sealed record FileImportResult(int RowsImported, int RowsSkipped, bool AssetWasCreated);

public sealed record DirectoryImportSummary(int FilesProcessed, int RowsImported, int RowsSkipped);

/// <summary>
/// The one place that knows how to turn a meter data file into persisted
/// readings (+ auto-create the asset if needed). Both the directory-scan
/// console app and the async upload background worker call into this -
/// neither has its own copy of this logic.
/// </summary>
public sealed class MeterDataImportService
{
    private readonly IEnumerable<IMeterDataParser> _parsers;
    private readonly IAssetRepository _assetRepository;
    private readonly IMeterReadingRepository _meterReadingRepository;
    private readonly MeterDataSettings _settings;
    private readonly ILogger<MeterDataImportService> _logger;

    public MeterDataImportService(
        IEnumerable<IMeterDataParser> parsers,
        IAssetRepository assetRepository,
        IMeterReadingRepository meterReadingRepository,
        IOptions<MeterDataSettings> settings,
        ILogger<MeterDataImportService> logger)
    {
        _parsers = parsers;
        _assetRepository = assetRepository;
        _meterReadingRepository = meterReadingRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<FileImportResult> ImportFileAsync(string filePath, string meterPointId, CancellationToken ct = default)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanParse(filePath))
            ?? throw new NotSupportedException($"No registered parser can handle '{Path.GetFileName(filePath)}'.");

        var parsedRecords = await parser.ParseAsync(filePath, ct);

        var assetWasCreated = await EnsureAssetExistsAsync(meterPointId, ct);

        var fileName = Path.GetFileName(filePath);
        var readings = parsedRecords
            .Select(r => new Domain.MeterData.MeterReading
            {
                MeterPointId = meterPointId,
                TimestampUtc = r.TimestampUtc,
                Production = r.Production,
                SourceFile = fileName,
            })
            .ToList();

        var (imported, skipped) = await _meterReadingRepository.UpsertManyAsync(readings, ct);

        _logger.LogInformation(
            "Imported {File}: {Imported} rows imported, {Skipped} skipped (asset created: {AssetCreated})",
            fileName, imported, skipped, assetWasCreated);

        return new FileImportResult(imported, skipped, assetWasCreated);
    }

    /// <summary>
    /// Scans the one fixed, configured meter data directory. Filenames
    /// are validated against MeterDataFileNaming before anything is
    /// touched - this is what the directory import route from the brief
    /// (as opposed to the UI upload route) actually runs.
    /// </summary>
    public async Task<DirectoryImportSummary> ImportAllInDirectoryAsync(CancellationToken ct = default)
    {
        var totalImported = 0;
        var totalSkipped = 0;
        var filesProcessed = 0;

        if (!System.IO.Directory.Exists(_settings.Directory))
        {
            _logger.LogWarning("Meter data directory '{Directory}' does not exist.", _settings.Directory);
            return new DirectoryImportSummary(0, 0, 0);
        }

        foreach (var filePath in System.IO.Directory.EnumerateFiles(_settings.Directory))
        {
            ct.ThrowIfCancellationRequested();

            var fileName = Path.GetFileName(filePath);
            if (!MeterDataFileNaming.TryParse(fileName, out var meterPointId, out _))
            {
                _logger.LogWarning("Skipping '{File}' - does not match the expected <meterPointId>.<csv|xlsx> naming.", fileName);
                continue;
            }

            var result = await ImportFileAsync(filePath, meterPointId, ct);
            filesProcessed++;
            totalImported += result.RowsImported;
            totalSkipped += result.RowsSkipped;
        }

        return new DirectoryImportSummary(filesProcessed, totalImported, totalSkipped);
    }

    /// <summary>Returns true if a new placeholder asset was created.</summary>
    private async Task<bool> EnsureAssetExistsAsync(string meterPointId, CancellationToken ct)
    {
        var existing = await _assetRepository.GetByMeterPointIdAsync(meterPointId, ct);
        if (existing is not null)
        {
            return false;
        }

        var placeholder = new UnclassifiedAsset { MeterPointId = meterPointId, Capacity = 0 };
        await _assetRepository.AddAsync(placeholder, ct);
        return true;
    }
}
