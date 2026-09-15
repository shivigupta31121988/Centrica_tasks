using Assets.Api.Dtos;
using Assets.Infrastructure.Settlement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Assets.Api.Controllers;

[ApiController]
[Route("api/settlements")]
[Authorize] // read-only - both Admin and Trader may view settlement figures
public sealed class SettlementsController : ControllerBase
{
    private readonly SettlementCalculator _calculator;
    private readonly SettlementSettings _settings;

    public SettlementsController(SettlementCalculator calculator, IOptions<SettlementSettings> settings)
    {
        _calculator = calculator;
        _settings = settings.Value;
    }

    /// <summary>Single asset, daily resolution.</summary>
    [HttpGet("assets/{assetId}")]
    public async Task<ActionResult<IReadOnlyList<DailySettlementDto>>> GetForAsset(
        string assetId, [FromQuery] DateOnly start, [FromQuery] DateOnly end, CancellationToken ct)
    {
        if (!TryValidateRange(start, end, _settings.MaxDailyRouteDays, out var rangeError))
        {
            return BadRequest(new { message = rangeError });
        }

        var results = await _calculator.CalculateForAssetAsync(assetId, start, end, ct);
        if (results is null)
        {
            return NotFound(new { message = $"No asset found with id '{assetId}'." });
        }

        return Ok(results.Select(r => new DailySettlementDto
        {
            Date = r.LocalDate.ToString("yyyy-MM-dd"),
            Amount = r.Amount,
            IncompleteHours = r.IncompleteHours,
        }).ToList());
    }

    /// <summary>All assets summed, monthly resolution.</summary>
    [HttpGet("total")]
    public async Task<ActionResult<IReadOnlyList<MonthlySettlementDto>>> GetTotal(
        [FromQuery] DateOnly start, [FromQuery] DateOnly end, CancellationToken ct)
    {
        if (!TryValidateRange(start, end, _settings.MaxMonthlyRouteDays, out var rangeError))
        {
            return BadRequest(new { message = rangeError });
        }

        var results = await _calculator.CalculateTotalAsync(start, end, ct);

        return Ok(results.Select(r => new MonthlySettlementDto
        {
            Month = $"{r.Year:D4}-{r.Month:D2}",
            Amount = r.Amount,
            IncompleteHours = r.IncompleteHours,
        }).ToList());
    }

    private static bool TryValidateRange(DateOnly start, DateOnly end, int maxDays, out string? error)
    {
        if (end < start)
        {
            error = "'end' must not be before 'start'.";
            return false;
        }

        if (end.DayNumber - start.DayNumber > maxDays)
        {
            error = $"Requested range exceeds the maximum of {maxDays} days.";
            return false;
        }

        error = null;
        return true;
    }
}
