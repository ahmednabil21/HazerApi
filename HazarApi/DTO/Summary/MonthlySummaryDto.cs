namespace HazarApi.DTO.Summary;

/// <summary>
/// ملخص أداء الموظف لشهر محدد.
/// </summary>
public class MonthlySummaryDto
{
    public int EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalTimeOffMinutesUsed { get; set; }
    public int TotalDelayMinutes { get; set; }
    public int NinetyMinutesConsumed { get; set; }
    public int RemainingTimeOffMinutes { get; set; }
    public int RemainingNinetyMinutes { get; set; }
    public int TotalOvertimeMinutes { get; set; }
    public DateTime CalculatedAt { get; set; }
}

