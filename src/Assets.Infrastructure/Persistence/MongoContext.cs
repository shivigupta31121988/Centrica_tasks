using Assets.Domain.Assets;
using Assets.Domain.Auth;
using Assets.Domain.MeterData;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Assets.Infrastructure.Persistence;

public sealed class MongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<Asset> Assets { get; }
    public IMongoCollection<User> Users { get; }
    public IMongoCollection<MeterReading> MeterReadings { get; }
    public IMongoCollection<ImportJob> ImportJobs { get; }

    private static bool _classMapsRegistered;
    private static bool _indexesEnsured;

    public MongoContext(IOptions<MongoSettings> settings)
    {
        RegisterClassMapsOnce();

        var client = new MongoClient(settings.Value.ConnectionString);
        Database = client.GetDatabase(settings.Value.DatabaseName);
        Assets = Database.GetCollection<Asset>(settings.Value.AssetsCollectionName);
        Users = Database.GetCollection<User>(settings.Value.UsersCollectionName);
        MeterReadings = Database.GetCollection<MeterReading>(settings.Value.MeterReadingsCollectionName);
        ImportJobs = Database.GetCollection<ImportJob>(settings.Value.ImportJobsCollectionName);

        EnsureIndexesOnce();
    }

    /// <summary>
    /// Composite uniqueness on (MeterPointId, TimestampUtc) is what makes
    /// re-running an import over the same file idempotent - a duplicate
    /// row is rejected/skipped rather than double-counted.
    /// </summary>
    private void EnsureIndexesOnce()
    {
        if (_indexesEnsured) return;

        var keys = Builders<MeterReading>.IndexKeys
            .Ascending(r => r.MeterPointId)
            .Ascending(r => r.TimestampUtc);

        MeterReadings.Indexes.CreateOne(new CreateIndexModel<MeterReading>(
            keys, new CreateIndexOptions { Unique = true }));

        _indexesEnsured = true;
    }

    /// <summary>
    /// Registers the discriminator so the driver can (de)serialize the
    /// polymorphic Asset hierarchy automatically. Adding a new asset type
    /// only requires a class map entry here plus the registry entry in
    /// AssetTypeRegistry - no repository/query code changes needed.
    /// </summary>
    private static void RegisterClassMapsOnce()
    {
        if (_classMapsRegistered) return;

        if (!BsonClassMap.IsClassMapRegistered(typeof(Asset)))
        {
            BsonClassMap.RegisterClassMap<Asset>(cm =>
            {
                cm.AutoMap();
                cm.SetIsRootClass(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(WindTurbine)))
        {
            BsonClassMap.RegisterClassMap<WindTurbine>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("WindTurbine");
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(SolarPanel)))
        {
            BsonClassMap.RegisterClassMap<SolarPanel>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("SolarPanel");
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(UnclassifiedAsset)))
        {
            BsonClassMap.RegisterClassMap<UnclassifiedAsset>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator("Unclassified");
            });
        }

        _classMapsRegistered = true;
    }
}
