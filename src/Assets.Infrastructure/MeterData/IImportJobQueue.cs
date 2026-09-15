namespace Assets.Infrastructure.MeterData;

public interface IImportJobQueue
{
    ValueTask EnqueueAsync(string jobId, CancellationToken ct = default);

    /// <summary>Blocks (asynchronously) until a job id is available.</summary>
    ValueTask<string> DequeueAsync(CancellationToken ct = default);
}
