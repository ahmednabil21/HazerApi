namespace HazarApi.DTO.Dashboard;

/// <summary>
/// بيانات المؤشرات العامة في لوحة الإدارة.
/// </summary>
public class DashboardStatsDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int TotalDelayMinutes { get; set; }
    public int TotalTimeOffMinutes { get; set; }
    public int TotalOvertimeMinutes { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

