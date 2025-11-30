namespace HazarApi.Entities;

/// <summary>
/// يضيف معلومات الأثر والتعطيل فوق المعرف الأساسي.
/// </summary>
public abstract class FullBaseEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

