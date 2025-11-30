using HazarApi.DTO.Common;
using HazarApi.DTO.Employees;
using HazarApi.IServices.Employees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HazarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<EmployeeDto>>> GetEmployees(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? includeInactive = null)
    {
        var result = await _employeeService.GetAsync(pageNumber, pageSize, searchTerm, includeInactive);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] EmployeeCreateDto dto)
    {
        var created = await _employeeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetEmployee), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, [FromBody] EmployeeUpdateDto dto)
    {
        var updated = await _employeeService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ApiResponseDto>> ToggleStatus(int id, [FromQuery] bool isActive)
    {
        var response = await _employeeService.ToggleStatusAsync(id, isActive);
        return Ok(response);
    }

    [HttpPut("{id:int}/reset-password")]
    public async Task<ActionResult<ApiResponseDto>> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
    {
        var response = await _employeeService.ResetPasswordAsync(id, dto);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponseDto>> SoftDelete(int id)
    {
        var response = await _employeeService.SoftDeleteAsync(id);
        return Ok(response);
    }

    [HttpGet("me/balance")]
    [Authorize]
    public async Task<ActionResult<EmployeeBalanceDto>> GetMyBalance()
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out var employeeId))
        {
            return Unauthorized();
        }

        var balance = await _employeeService.GetMyBalanceAsync(employeeId);
        return Ok(balance);
    }
}

