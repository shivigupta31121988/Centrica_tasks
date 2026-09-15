using Assets.Domain.MeterData;
using Assets.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Assets.Infrastructure.MeterData;

public sealed class MongoImportJobRepository : IImportJobRepository
{
    private readonly IMongoCollection<ImportJob> _collection;

    public MongoImportJobRepository(MongoContext context)
    {
        _collection = context.ImportJobs;
    }

    public Task AddAsync(ImportJob job, CancellationToken ct = default) =>
        _collection.InsertOneAsync(job, cancellationToken: ct);

    public async Task<ImportJob?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var filter = Builders<ImportJob>.Filter.Eq(j => j.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public Task UpdateAsync(ImportJob job, CancellationToken ct = default)
    {
        var filter = Builders<ImportJob>.Filter.Eq(j => j.Id, job.Id);
        return _collection.ReplaceOneAsync(filter, job, cancellationToken: ct);
    }
}
