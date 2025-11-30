namespace HazarApi.DTO.Attendance;

/// <summary>
/// إضافة زمنية جديدة.
/// </summary>
public class AddTimeOffDto
{
    public required DateOnly TimeOffDate { get; set; }
    public required int MinutesUsed { get; set; }
    public required string Reason { get; set; }
}

