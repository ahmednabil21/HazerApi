namespace HazarApi.DTO.Employees;

/// <summary>
/// نموذج تعديل بيانات الموظف.
/// </summary>
public class EmployeeUpdateDto
{
    public required string FullName { get; set; }
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "Employee";
}

