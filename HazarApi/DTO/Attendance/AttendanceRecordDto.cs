namespace HazarApi.DTO.Attendance;

/// <summary>
/// يمثل سجل الدوام بعد الحسابات.
/// </summary>
public class AttendanceRecordDto
{
    public int Id { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly CheckIn { get; set; }
    public TimeOnly CheckOut { get; set; }
    public int TimeOffMinutesUsed { get; set; }
    public int DelayMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public int NinetyMinutesDeducted { get; set; }
    public string? Notes { get; set; }
    public bool IsLocked { get; set; }
}

