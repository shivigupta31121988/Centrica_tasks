using Assets.Domain.MeterData;
using Assets.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Assets.Infrastructure.MeterData;

public sealed class MongoMeterReadingRepository : IMeterReadingRepository
{
    private readonly IMongoCollection<Domain.MeterData.MeterReading> _collection;

    public MongoMeterReadingRepository(MongoContext context)
    {
        _collection = context.MeterReadings;
    }

    public async Task<(int Imported, int Skipped)> UpsertManyAsync(
        IReadOnlyList<Domain.MeterData.MeterReading> readings, CancellationToken ct = default)
    {
        if (readings.Count == 0)
        {
            return (0, 0);
        }

        var models = readings.Select(r =>
        {
            var filter = Builders<Domain.MeterData.MeterReading>.Filter.And(
                Builders<Domain.MeterData.MeterReading>.Filter.Eq(x => x.MeterPointId, r.MeterPointId),
                Builders<Domain.MeterData.MeterReading>.Filter.Eq(x => x.TimestampUtc, r.TimestampUtc));

            // SetOnInsert only - an existing reading for this key is left
            // untouched rather than overwritten (idempotent re-import).
            var update = Builders<Domain.MeterData.MeterReading>.Update
                .SetOnInsert(x => x.Id, r.Id)
                .SetOnInsert(x => x.MeterPointId, r.MeterPointId)
                .SetOnInsert(x => x.TimestampUtc, r.TimestampUtc)
                .SetOnInsert(x => x.Production, r.Production)
                .SetOnInsert(x => x.SourceFile, r.SourceFile)
                .SetOnInsert(x => x.ImportedAtUtc, r.ImportedAtUtc);

            return new UpdateOneModel<Domain.MeterData.MeterReading>(filter, update) { IsUpsert = true };
        }).ToList();

        var result = await _collection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false }, ct);

        var imported = (int)result.Upserts.Count;
        var skipped = readings.Count - imported;
        return (imported, skipped);
    }

    public async Task<IReadOnlyList<Domain.MeterData.MeterReading>> GetByMeterPointIdInRangeAsync(
        string meterPointId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var filter = Builders<Domain.MeterData.MeterReading>.Filter.And(
            Builders<Domain.MeterData.MeterReading>.Filter.Eq(x => x.MeterPointId, meterPointId),
            Builders<Domain.MeterData.MeterReading>.Filter.Gte(x => x.TimestampUtc, startUtc),
            Builders<Domain.MeterData.MeterReading>.Filter.Lt(x => x.TimestampUtc, endUtc));

        return await _collection.Find(filter).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.MeterData.MeterReading>> GetAllInRangeAsync(
        DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var filter = Builders<Domain.MeterData.MeterReading>.Filter.And(
            Builders<Domain.MeterData.MeterReading>.Filter.Gte(x => x.TimestampUtc, startUtc),
            Builders<Domain.MeterData.MeterReading>.Filter.Lt(x => x.TimestampUtc, endUtc));

        return await _collection.Find(filter).ToListAsync(ct);
    }
}
