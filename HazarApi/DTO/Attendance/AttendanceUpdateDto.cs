namespace HazarApi.DTO.Attendance;

/// <summary>
/// يسمح للموظف بتعديل سجل نفس اليوم.
/// </summary>
public class AttendanceUpdateDto
{
    public required TimeOnly CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public int? TimeOffMinutesUsed { get; set; }
    public string? Notes { get; set; }
}

