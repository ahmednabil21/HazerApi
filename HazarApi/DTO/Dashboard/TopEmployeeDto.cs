namespace HazarApi.DTO.Dashboard;

/// <summary>
/// يمثل عنصرًا في قوائم الترتيب (تأخير أو التزام).
/// </summary>
public class TopEmployeeDto
{
    public int EmployeeId { get; set; }
    public required string EmployeeName { get; set; }
    public int Minutes { get; set; }
    public int Value { get; set; }
    public string Metric { get; set; } = string.Empty;
}

