namespace HazarApi.Entities;

/// <summary>
/// يمثل إعدادات الدوام العامة التي يستخدمها النظام في الحسابات.
/// </summary>
public class AttendancePolicy : FullBaseEntity
{
    public TimeOnly WorkdayStart { get; set; } = new(7, 0);
    public TimeOnly WorkdayEnd { get; set; } = new(14, 0);
    public int MonthlyTimeOffAllowance { get; set; } = 420;
    public int NinetyMinutesAllowance { get; set; } = 90;
    public bool IsActive { get; set; } = true;
}

