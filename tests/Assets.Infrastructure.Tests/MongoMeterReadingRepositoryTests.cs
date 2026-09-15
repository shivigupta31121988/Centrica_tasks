using Assets.Domain.MeterData;
using Assets.Infrastructure.MeterData;
using FluentAssertions;
using Xunit;

namespace Assets.Infrastructure.Tests;

public class MongoMeterReadingRepositoryTests : IClassFixture<MongoFixture>
{
    private readonly MongoMeterReadingRepository _repository;

    public MongoMeterReadingRepositoryTests(MongoFixture fixture)
    {
        _repository = new MongoMeterReadingRepository(fixture.Context);
    }

    [Fact]
    public async Task UpsertManyAsync_NewReadings_AllImported()
    {
        var meterPointId = $"mp-{Guid.NewGuid()}";
        var readings = new List<MeterReading>
        {
            new() { MeterPointId = meterPointId, TimestampUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), Production = 1 },
            new() { MeterPointId = meterPointId, TimestampUtc = new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc), Production = 2 },
        };

        var (imported, skipped) = await _repository.UpsertManyAsync(readings);

        imported.Should().Be(2);
        skipped.Should().Be(0);
    }

    [Fact]
    public async Task UpsertManyAsync_ReRunningSameFile_SkipsAlreadyImportedRows()
    {
        var meterPointId = $"mp-{Guid.NewGuid()}";
        var readings = new List<MeterReading>
        {
            new() { MeterPointId = meterPointId, TimestampUtc = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc), Production = 5 },
        };

        var first = await _repository.UpsertManyAsync(readings);
        first.Imported.Should().Be(1);

        // Same (MeterPointId, TimestampUtc) key again - simulates re-running
        // the importer over the same file.
        var second = await _repository.UpsertManyAsync(readings);

        second.Imported.Should().Be(0);
        second.Skipped.Should().Be(1);
    }
}
