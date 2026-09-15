namespace Assets.Domain.MeterData;

/// <summary>
/// Implement this once per supported file format. The import pipeline
/// (MeterDataImportService) never branches on format itself - it asks
/// every registered parser CanParse(filePath) and uses whichever says
/// yes. Adding a new format later (JSON, XML, a different provider's
/// layout) means writing one new class and registering it in DI -
/// nothing else in the system changes.
/// </summary>
public interface IMeterDataParser
{
    bool CanParse(string filePath);

    Task<IReadOnlyList<MeterReadingRecord>> ParseAsync(string filePath, CancellationToken ct = default);
}
