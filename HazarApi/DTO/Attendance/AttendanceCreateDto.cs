namespace HazarApi.DTO.Attendance;

/// <summary>
/// يمثل بيانات إنشاء سجل دوام جديد.
/// </summary>
public class AttendanceCreateDto
{
    public required DateOnly WorkDate { get; set; }
    public required TimeOnly CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public int? TimeOffMinutesUsed { get; set; }
    public string? Notes { get; set; }
}

