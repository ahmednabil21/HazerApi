namespace HazarApi.DTO.Common;

/// <summary>
/// رد عام لواجهات REST لتوحيد الرسائل.
/// </summary>
public class ApiResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

