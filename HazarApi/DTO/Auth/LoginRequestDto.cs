namespace HazarApi.DTO.Auth;

/// <summary>
/// يمثل بيانات نموذج تسجيل الدخول.
/// </summary>
public class LoginRequestDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

