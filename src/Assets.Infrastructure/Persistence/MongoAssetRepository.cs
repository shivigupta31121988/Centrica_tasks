using Assets.Domain.Assets;
using MongoDB.Driver;

namespace Assets.Infrastructure.Persistence;

/// <summary>
/// All queries use the typed Builders&lt;Asset&gt;.Filter API rather than
/// raw/string-built queries. This is a deliberate security choice: it
/// removes the NoSQL-injection risk of ever interpolating user input into
/// a query (e.g. a user-supplied string being interpreted as a Mongo
/// operator like $where or $gt).
/// </summary>
public sealed class MongoAssetRepository : IAssetRepository
{
    private readonly IMongoCollection<Asset> _collection;

    public MongoAssetRepository(MongoContext context)
    {
        _collection = context.Assets;
    }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default)
    {
        var results = await _collection.Find(FilterDefinition<Asset>.Empty).ToListAsync(ct);
        return results;
    }

    public async Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<Asset>.Filter.Eq(a => a.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<Asset?> GetByMeterPointIdAsync(string meterPointId, CancellationToken ct = default)
    {
        var filter = Builders<Asset>.Filter.Eq(a => a.MeterPointId, meterPointId);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public Task AddAsync(Asset asset, CancellationToken ct = default) =>
        _collection.InsertOneAsync(asset, cancellationToken: ct);
}
