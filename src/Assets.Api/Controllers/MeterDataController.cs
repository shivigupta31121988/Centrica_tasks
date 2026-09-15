using Assets.Api.Dtos;
using Assets.Domain.Auth;
using Assets.Domain.MeterData;
using Assets.Infrastructure.MeterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Assets.Api.Controllers;

[ApiController]
[Route("api/meter-data")]
[Authorize(Roles = nameof(UserRole.Admin))] // importing data is a privileged operation, same tier as creating assets
public sealed class MeterDataController : ControllerBase
{
    private readonly IImportJobRepository _jobRepository;
    private readonly IImportJobQueue _queue;
    private readonly MeterDataSettings _settings;
    private readonly ILogger<MeterDataController> _logger;

    public MeterDataController(
        IImportJobRepository jobRepository,
        IImportJobQueue queue,
        IOptions<MeterDataSettings> settings,
        ILogger<MeterDataController> logger)
    {
        _jobRepository = jobRepository;
        _queue = queue;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Accepts a meter data file, saves it, queues it for background
    /// processing, and returns immediately with a job id to poll -
    /// this is what lets a large file scale rather than tying up the
    /// request until parsing/persisting finishes.
    /// </summary>
    [HttpPost("import")]
    [RequestFormLimits(MultipartBodyLengthLimit = 25 * 1024 * 1024)] // headroom above the 20MB app-level check below
    public async Task<ActionResult<ImportAcceptedDto>> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No file was uploaded." });
        }

        if (file.Length > _settings.MaxUploadSizeBytes)
        {
            return BadRequest(new { message = $"File exceeds the maximum allowed size of {_settings.MaxUploadSizeBytes / (1024 * 1024)}MB." });
        }

        // The original uploaded filename's raw text is never used to build
        // a filesystem path - only the validated meterPointId + a
        // server-controlled extension are. This is what actually prevents
        // path traversal via a crafted upload filename.
        if (!MeterDataFileNaming.TryParse(file.FileName, out var meterPointId, out var extension))
        {
            return BadRequest(new { message = "Filename must be in the form <meterPointId>.csv or <meterPointId>.xlsx." });
        }

        Directory.CreateDirectory(_settings.Directory);
        var safeFileName = MeterDataFileNaming.BuildFileName(meterPointId, extension);
        var destinationPath = Path.Combine(_settings.Directory, safeFileName);

        await using (var stream = System.IO.File.Create(destinationPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var job = new ImportJob
        {
            FileName = safeFileName,
            MeterPointId = meterPointId,
            UploadedBy = User.Identity?.Name ?? "unknown",
        };
        await _jobRepository.AddAsync(job, ct);
        await _queue.EnqueueAsync(job.Id, ct);

        _logger.LogInformation("Queued import job {JobId} for file {FileName}", job.Id, safeFileName);

        return Accepted(new ImportAcceptedDto { JobId = job.Id });
    }

    [HttpGet("import/{jobId}")]
    public async Task<ActionResult<ImportJobStatusDto>> GetStatus(string jobId, CancellationToken ct)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(new ImportJobStatusDto
        {
            JobId = job.Id,
            FileName = job.FileName,
            Status = job.Status.ToString(),
            RowsImported = job.RowsImported,
            RowsSkipped = job.RowsSkipped,
            ElapsedSeconds = job.GetElapsedSeconds(DateTime.UtcNow),
            ErrorMessage = job.ErrorMessage,
        });
    }
}
