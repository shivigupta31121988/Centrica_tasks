using Assets.Domain.MeterData;
using FluentAssertions;
using Xunit;

namespace Assets.Domain.Tests;

public class ImportJobTests
{
    [Fact]
    public void GetElapsedSeconds_BeforeStarted_ReturnsZero()
    {
        var job = new ImportJob();

        job.GetElapsedSeconds(DateTime.UtcNow).Should().Be(0);
    }

    [Fact]
    public void GetElapsedSeconds_WhileProcessing_MeasuresAgainstNow()
    {
        var started = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var job = new ImportJob { StartedAtUtc = started };

        var now = started.AddSeconds(7.5);

        job.GetElapsedSeconds(now).Should().BeApproximately(7.5, 0.001);
    }

    [Fact]
    public void GetElapsedSeconds_AfterCompletion_IsFixedRegardlessOfNow()
    {
        var started = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var completed = started.AddSeconds(3.2);
        var job = new ImportJob { StartedAtUtc = started, CompletedAtUtc = completed };

        // Calling this "later" should not change the answer - the job is done.
        var muchLater = completed.AddMinutes(10);

        job.GetElapsedSeconds(muchLater).Should().BeApproximately(3.2, 0.001);
    }
}
