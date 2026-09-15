using Assets.Domain.MeterData;
using Assets.Infrastructure.MeterData;
using Microsoft.Extensions.Options;

namespace Assets.Api.BackgroundServices;

/// <summary>
/// Drains IImportJobQueue and runs each job through MeterDataImportService.
/// A new DI scope per job because the repositories/service are registered
/// scoped, but this worker itself is a singleton hosted service.
/// </summary>
public sealed class ImportJobWorker : BackgroundService
{
    private readonly IImportJobQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MeterDataSettings _settings;
    private readonly ILogger<ImportJobWorker> _logger;

    public ImportJobWorker(
        IImportJobQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<MeterDataSettings> settings,
        ILogger<ImportJobWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string jobId;
            try
            {
                jobId = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await ProcessJobAsync(jobId, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(string jobId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IImportJobRepository>();
        var importService = scope.ServiceProvider.GetRequiredService<MeterDataImportService>();

        var job = await jobRepository.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            _logger.LogWarning("Import job {JobId} not found - skipping.", jobId);
            return;
        }

        job.Status = ImportJobStatus.Processing;
        job.StartedAtUtc = DateTime.UtcNow;
        await jobRepository.UpdateAsync(job, ct);

        try
        {
            var filePath = Path.Combine(_settings.Directory, job.FileName);
            var result = await importService.ImportFileAsync(filePath, job.MeterPointId, ct);

            job.RowsImported = result.RowsImported;
            job.RowsSkipped = result.RowsSkipped;
            job.Status = ImportJobStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import job {JobId} failed for file {FileName}", jobId, job.FileName);
            job.Status = ImportJobStatus.Failed;
            job.ErrorMessage = "The file could not be imported. Check the file format and try again.";
        }
        finally
        {
            job.CompletedAtUtc = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job, ct);
        }
    }
}
