namespace HazarApi.DTO.Employees;

/// <summary>
/// بيانات إنشاء موظف جديد.
/// </summary>
public class EmployeeCreateDto
{
    public required string FullName { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? JobTitle { get; set; }
    public string Role { get; set; } = "Employee";
}

