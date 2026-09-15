using System.Text;
using Assets.Api.Controllers;
using Assets.Api.Dtos;
using Assets.Domain.MeterData;
using Assets.Infrastructure.MeterData;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Assets.Api.Tests;

public class MeterDataControllerTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory().FullName;
    private readonly Mock<IImportJobRepository> _jobRepository = new();
    private readonly Mock<IImportJobQueue> _queue = new();
    private readonly MeterDataController _controller;

    public MeterDataControllerTests()
    {
        var settings = Options.Create(new MeterDataSettings { Directory = _tempDir, MaxUploadSizeBytes = 1024 });
        _controller = new MeterDataController(_jobRepository.Object, _queue.Object, settings, Mock.Of<ILogger<MeterDataController>>());

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
    }

    [Fact]
    public async Task Upload_WithInvalidFileName_ReturnsBadRequest()
    {
        var file = CreateFormFile("not-a-meter-point.csv", "Timestamp,Production\n2024-01-01T00:00:00Z,1\n");

        var result = await _controller.Upload(file, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _queue.Verify(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Upload_ExceedingMaxSize_ReturnsBadRequest()
    {
        var oversized = new string('x', 2000);
        var file = CreateFormFile("12345.csv", oversized);

        var result = await _controller.Upload(file, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_WithNoFile_ReturnsBadRequest()
    {
        var result = await _controller.Upload(null, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_ValidFile_SavesFileCreatesJobAndEnqueues()
    {
        var file = CreateFormFile("570715000000088747.csv", "Timestamp,Production\n2024-01-01T00:00:00Z,1\n");

        var result = await _controller.Upload(file, CancellationToken.None);

        result.Result.Should().BeOfType<AcceptedResult>();
        var accepted = (AcceptedResult)result.Result!;
        var dto = accepted.Value as ImportAcceptedDto;
        dto!.JobId.Should().NotBeNullOrEmpty();

        File.Exists(Path.Combine(_tempDir, "570715000000088747.csv")).Should().BeTrue();
        _jobRepository.Verify(r => r.AddAsync(It.Is<ImportJob>(j => j.MeterPointId == "570715000000088747"), It.IsAny<CancellationToken>()), Times.Once);
        _queue.Verify(q => q.EnqueueAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatus_UnknownJobId_ReturnsNotFound()
    {
        _jobRepository.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((ImportJob?)null);

        var result = await _controller.GetStatus("missing", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetStatus_ExistingJob_ReturnsComputedElapsedSeconds()
    {
        var job = new ImportJob
        {
            Id = "job-1",
            FileName = "123.csv",
            Status = ImportJobStatus.Processing,
            StartedAtUtc = DateTime.UtcNow.AddSeconds(-5),
        };
        _jobRepository.Setup(r => r.GetByIdAsync("job-1", It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var result = await _controller.GetStatus("job-1", CancellationToken.None);

        var ok = result.Result as OkObjectResult;
        var dto = ok!.Value as ImportJobStatusDto;
        dto!.Status.Should().Be("Processing");
        dto.ElapsedSeconds.Should().BeGreaterOrEqualTo(5);
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
