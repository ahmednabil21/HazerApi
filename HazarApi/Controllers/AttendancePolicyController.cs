using HazarApi.DTO.Policies;
using HazarApi.IServices.Policies;
using Microsoft.AspNetCore.Mvc;

namespace HazarApi.Controllers;

[ApiController]
[Route("api/policies/attendance")]
public class AttendancePolicyController : ControllerBase
{
    private readonly IAttendancePolicyService _policyService;

    public AttendancePolicyController(IAttendancePolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpGet]
    public async Task<ActionResult<AttendancePolicyDto>> GetPolicy()
    {
        var policy = await _policyService.GetActivePolicyAsync();
        return Ok(policy);
    }

    [HttpPut]
    public async Task<ActionResult<AttendancePolicyDto>> UpdatePolicy([FromBody] AttendancePolicyDto dto)
    {
        var policy = await _policyService.UpdateAsync(dto);
        return Ok(policy);
    }
}

