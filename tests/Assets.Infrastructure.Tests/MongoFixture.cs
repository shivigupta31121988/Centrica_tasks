using Assets.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Mongo2Go;

namespace Assets.Infrastructure.Tests;

/// <summary>
/// Spins up a real, ephemeral mongod process for the duration of the test
/// class (via Mongo2Go) so repository tests exercise real Mongo query
/// semantics rather than a hand-rolled mock of the driver.
/// </summary>
public sealed class MongoFixture : IDisposable
{
    private readonly MongoDbRunner _runner;
    public MongoContext Context { get; }

    public MongoFixture()
    {
        _runner = MongoDbRunner.Start();

        var settings = Options.Create(new MongoSettings
        {
            ConnectionString = _runner.ConnectionString,
            DatabaseName = "test_renewable_assets",
        });

        Context = new MongoContext(settings);
    }

    public void Dispose() => _runner.Dispose();
}
