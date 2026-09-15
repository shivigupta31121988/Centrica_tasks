using Assets.Domain.Assets;

namespace Assets.Infrastructure.Persistence;

public interface IAssetRepository
{
    Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default);

    Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default);

    Task<Asset?> GetByMeterPointIdAsync(string meterPointId, CancellationToken ct = default);

    Task AddAsync(Asset asset, CancellationToken ct = default);
}
