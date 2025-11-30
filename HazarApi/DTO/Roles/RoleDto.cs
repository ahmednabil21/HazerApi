namespace HazarApi.DTO.Roles;

/// <summary>
/// يعبر عن الدور المتاح في النظام.
/// </summary>
public class RoleDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

