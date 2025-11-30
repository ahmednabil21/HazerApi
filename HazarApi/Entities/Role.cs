namespace HazarApi.Entities;

/// <summary>
/// يمثل الدور (Admin أو Employee فقط).
/// </summary>
public class Role : BaseEntity
{
    public RoleType Type { get; set; }
    public string? Description { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}

