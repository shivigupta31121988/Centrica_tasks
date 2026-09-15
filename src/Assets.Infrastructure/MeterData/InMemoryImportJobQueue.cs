using System.Threading.Channels;

namespace Assets.Infrastructure.MeterData;

/// <summary>
/// PRODUCTION NOTE: this is an in-process, in-memory queue - it does not
/// survive an app restart/crash mid-job. It's the deliberate MVP choice
/// (matches the plan doc). Because callers only depend on IImportJobQueue,
/// swapping this for a durable queue (e.g. AWS SQS, with the worker as a
/// separate process/ECS task) later is a DI registration change, not a
/// redesign.
/// </summary>
public sealed class InMemoryImportJobQueue : IImportJobQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

    public ValueTask EnqueueAsync(string jobId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(jobId, ct);

    public ValueTask<string> DequeueAsync(CancellationToken ct = default) =>
        _channel.Reader.ReadAsync(ct);
}
