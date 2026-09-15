using Assets.Domain.MeterData;

namespace Assets.Infrastructure.MeterData;

public interface IMeterReadingRepository
{
    /// <summary>
    /// Upserts by (MeterPointId, TimestampUtc). A reading that already
    /// exists for that key is left untouched (not overwritten) - this is
    /// what makes re-running an import over the same file idempotent.
    /// Returns (rowsImported, rowsSkipped).
    /// </summary>
    Task<(int Imported, int Skipped)> UpsertManyAsync(IReadOnlyList<MeterReading> readings, CancellationToken ct = default);

    /// <summary>All readings for one meter point within [startUtc, endUtc), used by the single-asset settlement route.</summary>
    Task<IReadOnlyList<MeterReading>> GetByMeterPointIdInRangeAsync(string meterPointId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);

    /// <summary>All readings across every meter point within [startUtc, endUtc), used by the total settlement route.</summary>
    Task<IReadOnlyList<MeterReading>> GetAllInRangeAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
}
