namespace HazarApi.Entities;

/// <summary>
/// يمثل موظف النظام مع بيانات الدخول ورصيد الزمنيات.
/// </summary>
public class Employee : FullBaseEntity
{
    public required string FullName { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; } = true;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public int MonthlyTimeOffBalance { get; set; } = 420; // دقائق الزمنية الافتراضية
    public int NinetyMinutesBalance { get; set; } = 90;

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<MonthlySummary> MonthlySummaries { get; set; } = new List<MonthlySummary>();
}

