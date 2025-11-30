namespace HazarApi.DTO.Policies;

/// <summary>
/// يمثل إعدادات الدوام القابلة للتعديل من قبل المدير.
/// </summary>
public class AttendancePolicyDto
{
    public int Id { get; set; }
    public TimeOnly WorkdayStart { get; set; } = new(7, 0);
    public TimeOnly WorkdayEnd { get; set; } = new(14, 0);
    public int MonthlyTimeOffAllowance { get; set; } = 420;
    public int NinetyMinutesAllowance { get; set; } = 90;
    public bool IsActive { get; set; } = true;
}

