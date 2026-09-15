using Assets.Api.Dtos;
using Assets.Api.Mapping;
using Assets.Domain.Auth;
using Assets.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Assets.Api.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize] // any authenticated user (Admin or Trader) may view
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetRepository _assetRepository;
    private readonly ILogger<AssetsController> _logger;

    public AssetsController(IAssetRepository assetRepository, ILogger<AssetsController> logger)
    {
        _assetRepository = assetRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetAll(CancellationToken ct)
    {
        var assets = await _assetRepository.GetAllAsync(ct);
        return Ok(assets.Select(AssetMapper.ToDto).ToList());
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<AssetDto>> Create([FromBody] CreateAssetDto request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var asset = AssetMapper.ToDomain(request);
        if (asset is null)
        {
            return BadRequest(new { message = $"Unknown asset type '{request.Type}'." });
        }

        var existing = await _assetRepository.GetByMeterPointIdAsync(request.MeterPointId, ct);
        if (existing is not null)
        {
            return Conflict(new { message = $"An asset with meter point id '{request.MeterPointId}' already exists." });
        }

        await _assetRepository.AddAsync(asset, ct);

        var userName = User?.Identity?.Name ?? "anonymous";
        _logger.LogInformation(
            "Asset created: type={AssetType} meterPointId={MeterPointId} by user={User}",
            asset.AssetType, asset.MeterPointId, userName);

        var dto = AssetMapper.ToDto(asset);
        return CreatedAtAction(nameof(GetAll), new { }, dto);
    }
}
