using Assets.Domain.MeterData;

namespace Assets.Infrastructure.MeterData;

public interface IImportJobRepository
{
    Task AddAsync(ImportJob job, CancellationToken ct = default);

    Task<ImportJob?> GetByIdAsync(string id, CancellationToken ct = default);

    Task UpdateAsync(ImportJob job, CancellationToken ct = default);
}
