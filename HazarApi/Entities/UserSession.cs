namespace HazarApi.Entities;

/// <summary>
/// يمثل جلسة دخول/خروج موظف للنظام.
/// </summary>
public class UserSession : FullBaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime LoginTime { get; set; } = DateTime.UtcNow;
    public DateTime? LogoutTime { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; } = true; // الجلسة ما زالت نشطة
}

