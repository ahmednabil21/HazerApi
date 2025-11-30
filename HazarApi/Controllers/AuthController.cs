using HazarApi.DTO.Auth;
using HazarApi.DTO.Common;
using HazarApi.IServices.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HazarApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        var response = await _authService.LoginAsync(dto, ipAddress, userAgent);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var response = await _authService.RefreshTokenAsync(dto);
        return Ok(response);
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<UserSessionDto>>> GetSessions([FromQuery] int? days = 30)
    {
        var employeeIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out var employeeId))
        {
            return Unauthorized();
        }

        var sessions = await _authService.GetUserSessionsAsync(employeeId, days);
        return Ok(sessions);
    }
}

