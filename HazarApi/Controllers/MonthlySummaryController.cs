using HazarApi.DTO.Common;
using HazarApi.DTO.Summary;
using HazarApi.IServices.Summary;
using Microsoft.AspNetCore.Mvc;

namespace HazarApi.Controllers;

[ApiController]
[Route("api/summary")]
public class MonthlySummaryController : ControllerBase
{
    private readonly IMonthlySummaryService _summaryService;

    public MonthlySummaryController(IMonthlySummaryService summaryService)
    {
        _summaryService = summaryService;
    }

    [HttpGet("{employeeId:int}/{year:int}/{month:int}")]
    public async Task<ActionResult<MonthlySummaryDto>> GetSummary(int employeeId, int year, int month)
    {
        var summary = await _summaryService.GetAsync(employeeId, year, month);
        if (summary is null)
        {
            return NotFound();
        }

        return Ok(summary);
    }

    [HttpPost("recalculate/{employeeId:int}/{year:int}/{month:int}")]
    public async Task<ActionResult<ApiResponseDto>> Recalculate(int employeeId, int year, int month)
    {
        var response = await _summaryService.RecalculateAsync(new RecalculateSummaryRequestDto
        {
            EmployeeId = employeeId,
            Year = year,
            Month = month
        });

        return Ok(response);
    }
}

