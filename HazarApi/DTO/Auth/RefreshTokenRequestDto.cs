namespace HazarApi.DTO.Auth;

/// <summary>
/// طلب تجديد التوكن.
/// </summary>
public class RefreshTokenRequestDto
{
    public required string RefreshToken { get; set; }
}

