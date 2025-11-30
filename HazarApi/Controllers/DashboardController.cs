using HazarApi.DTO.Dashboard;
using HazarApi.IServices.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace HazarApi.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        var stats = await _dashboardService.GetStatsAsync(year, month);
        return Ok(stats);
    }

    [HttpGet("top-delays")]
    public async Task<ActionResult<IReadOnlyCollection<TopEmployeeDto>>> GetTopDelays(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int take = 5)
    {
        var list = await _dashboardService.GetTopDelaysAsync(year, month, take);
        return Ok(list);
    }

    [HttpGet("top-commitment")]
    public async Task<ActionResult<IReadOnlyCollection<TopEmployeeDto>>> GetTopCommitment(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] int take = 5)
    {
        var list = await _dashboardService.GetTopCommitmentAsync(year, month, take);
        return Ok(list);
    }
}

