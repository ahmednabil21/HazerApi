using HazarApi.DTO.Attendance;
using HazarApi.DTO.Common;
using HazarApi.IServices.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HazarApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost]
    public async Task<ActionResult<AttendanceRecordDto>> Create(
        [FromQuery] int employeeId,
        [FromBody] AttendanceCreateDto dto)
    {
        var record = await _attendanceService.CreateAsync(employeeId, dto);
        return Ok(record);
    }

    [HttpPut("{attendanceId:int}")]
    public async Task<ActionResult<AttendanceRecordDto>> Update(
        int attendanceId,
        [FromQuery] int employeeId,
        [FromBody] AttendanceUpdateDto dto)
    {
        var record = await _attendanceService.UpdateAsync(employeeId, attendanceId, dto);
        return Ok(record);
    }

    [HttpGet("{employeeId:int}/{year:int}/{month:int}")]
    public async Task<ActionResult<IReadOnlyCollection<AttendanceRecordDto>>> GetMonthly(
        int employeeId,
        int year,
        int month,
        [FromQuery] bool includeLocked = false)
    {
        var records = await _attendanceService.GetMonthlyAsync(employeeId, year, month, includeLocked);
        return Ok(records);
    }

    [HttpPost("check-in")]
    [Authorize]
    public async Task<ActionResult<AttendanceRecordDto>> CheckIn([FromBody] CheckInDto dto)
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out var employeeId))
        {
            return Unauthorized();
        }

        var record = await _attendanceService.CheckInAsync(employeeId, dto);
        return Ok(record);
    }

    [HttpPost("check-out")]
    [Authorize]
    public async Task<ActionResult<AttendanceRecordDto>> CheckOut([FromBody] CheckOutDto dto)
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out var employeeId))
        {
            return Unauthorized();
        }

        var record = await _attendanceService.CheckOutAsync(employeeId, dto);
        return Ok(record);
    }

    [HttpPost("time-off")]
    [Authorize]
    public async Task<ActionResult<ApiResponseDto>> AddTimeOff([FromBody] AddTimeOffDto dto)
    {
        var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out var employeeId))
        {
            return Unauthorized();
        }

        var result = await _attendanceService.AddTimeOffAsync(employeeId, dto);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

