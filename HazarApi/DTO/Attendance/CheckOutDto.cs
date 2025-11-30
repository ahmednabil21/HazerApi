namespace HazarApi.DTO.Attendance;

/// <summary>
/// تسجيل خروج الموظف.
/// </summary>
public class CheckOutDto
{
    public required DateOnly WorkDate { get; set; }
    public required TimeOnly CheckOutTime { get; set; }
    public string? Notes { get; set; }
}

