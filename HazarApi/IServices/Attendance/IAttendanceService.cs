using HazarApi.DTO.Attendance;
using HazarApi.DTO.Common;

namespace HazarApi.IServices.Attendance;

public interface IAttendanceService
{
    Task<AttendanceRecordDto> CreateAsync(int employeeId, AttendanceCreateDto dto);
    Task<AttendanceRecordDto> UpdateAsync(int employeeId, int attendanceId, AttendanceUpdateDto dto);
    Task<IReadOnlyCollection<AttendanceRecordDto>> GetMonthlyAsync(int employeeId, int year, int month, bool includeLocked = false);
    Task<AttendanceRecordDto> CheckInAsync(int employeeId, CheckInDto dto);
    Task<AttendanceRecordDto> CheckOutAsync(int employeeId, CheckOutDto dto);
    Task<ApiResponseDto> AddTimeOffAsync(int employeeId, AddTimeOffDto dto);
}

