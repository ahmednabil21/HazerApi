using HazarApi.DTO.Common;
using HazarApi.DTO.Summary;

namespace HazarApi.IServices.Summary;

public interface IMonthlySummaryService
{
    Task<MonthlySummaryDto?> GetAsync(int employeeId, int year, int month);
    Task<ApiResponseDto> RecalculateAsync(RecalculateSummaryRequestDto dto);
}

