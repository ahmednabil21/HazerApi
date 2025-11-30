namespace HazarApi.Entities;

/// <summary>
/// يحفظ مجموعات الأداء الشهرية لكل موظف.
/// </summary>
public class MonthlySummary : FullBaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    public int TotalTimeOffMinutesUsed { get; set; }
    public int TotalDelayMinutes { get; set; }
    public int NinetyMinutesConsumed { get; set; }
    public int RemainingTimeOffMinutes { get; set; }
    public int RemainingNinetyMinutes { get; set; }
    public int TotalOvertimeMinutes { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

