namespace HazarApi.DTO.Auth;

/// <summary>
/// يمثل جلسة مستخدم.
/// </summary>
public class UserSessionDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LogoutTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; }
}

