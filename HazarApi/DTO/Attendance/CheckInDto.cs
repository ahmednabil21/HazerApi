namespace HazarApi.DTO.Attendance;

/// <summary>
/// تسجيل دخول الموظف.
/// </summary>
public class CheckInDto
{
    public required DateOnly WorkDate { get; set; }
    public required TimeOnly CheckInTime { get; set; }
    public int? TimeOffMinutesUsed { get; set; }
    public string? Notes { get; set; }
}

