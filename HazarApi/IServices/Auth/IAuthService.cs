using HazarApi.DTO.Auth;
using HazarApi.DTO.Common;

namespace HazarApi.IServices.Auth;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null, string? userAgent = null);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);
    Task<ApiResponseDto> LogoutAsync(int employeeId);
    Task<IReadOnlyCollection<UserSessionDto>> GetUserSessionsAsync(int employeeId, int? days = 30);
}

