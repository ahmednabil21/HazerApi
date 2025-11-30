namespace HazarApi.Entities;

/// <summary>
/// يمثل الجذر الموحد لكل الكيانات مع معرف رقمي.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}

