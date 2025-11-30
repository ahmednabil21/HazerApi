namespace HazarApi.DTO.Common;

/// <summary>
/// نموذج قياسي لنتائج الاستعلامات مع ترقيم الصفحات.
/// </summary>
public class PagedResultDto<T>
{
    public required IReadOnlyCollection<T> Items { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

