namespace HazarApi.Entities;

/// <summary>
/// يمثل سجل حضور يومي لموظف محدد.
/// </summary>
public class AttendanceRecord : FullBaseEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

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

