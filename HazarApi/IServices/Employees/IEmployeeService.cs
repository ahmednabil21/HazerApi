using HazarApi.DTO.Common;
using HazarApi.DTO.Employees;

namespace HazarApi.IServices.Employees;

public interface IEmployeeService
{
    Task<PagedResultDto<EmployeeDto>> GetAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? includeInactive = null);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(EmployeeCreateDto dto);
    Task<EmployeeDto> UpdateAsync(int id, EmployeeUpdateDto dto);
    Task<ApiResponseDto> ToggleStatusAsync(int id, bool isActive);
    Task<ApiResponseDto> ResetPasswordAsync(int id, ResetPasswordDto dto);
    Task<ApiResponseDto> SoftDeleteAsync(int id);
    Task<EmployeeBalanceDto> GetMyBalanceAsync(int employeeId);
}

