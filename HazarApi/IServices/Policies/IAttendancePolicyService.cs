using HazarApi.DTO.Policies;

namespace HazarApi.IServices.Policies;

public interface IAttendancePolicyService
{
    Task<AttendancePolicyDto> GetActivePolicyAsync();
    Task<AttendancePolicyDto> UpdateAsync(AttendancePolicyDto dto);
}

