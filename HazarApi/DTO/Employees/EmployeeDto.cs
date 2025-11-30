namespace HazarApi.DTO.Employees;

/// <summary>
/// يعرض بيانات الموظف للإدارة.
/// </summary>
public class EmployeeDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Username { get; set; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "Employee";
    public int MonthlyTimeOffBalance { get; set; }
    public int NinetyMinutesBalance { get; set; }
}

