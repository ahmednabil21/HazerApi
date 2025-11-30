namespace HazarApi.DTO.Summary;

/// <summary>
/// طلب لإعادة حساب شهر كامل.
/// </summary>
public class RecalculateSummaryRequestDto
{
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}

