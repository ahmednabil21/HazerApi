namespace HazarApi.DTO.Auth;

/// <summary>
/// يمثل نتيجة المصادقة الناجحة.
/// </summary>
public class AuthResponseDto
{
    public required string AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }

    public required string Username { get; set; }
    public required string Role { get; set; }
}

