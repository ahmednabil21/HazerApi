using HazarApi.Data;
using HazarApi.DTO.Common;
using HazarApi.DTO.Summary;
using HazarApi.Entities;
using HazarApi.IServices.Summary;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HazarApi.Services.Summary;

public class MonthlySummaryService : IMonthlySummaryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MonthlySummaryService> _logger;
    private readonly AutoMapper.IMapper _mapper;

    public MonthlySummaryService(AppDbContext context, ILogger<MonthlySummaryService> logger, AutoMapper.IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<MonthlySummaryDto?> GetAsync(int employeeId, int year, int month)
    {
        _logger.LogInformation("Retrieving summary for employee {EmployeeId} {Year}-{Month}", employeeId, year, month);

        var summary = await _context.MonthlySummaries
            .Include(s => s.Employee)
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Year == year && s.Month == month && !s.IsDeleted);

        if (summary == null)
        {
            // Create summary if it doesn't exist
            await RecalculateSummaryAsync(employeeId, year, month);
            summary = await _context.MonthlySummaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Year == year && s.Month == month && !s.IsDeleted);
        }

        return summary == null ? null : _mapper.Map<MonthlySummaryDto>(summary);
    }

    public async Task<ApiResponseDto> RecalculateAsync(RecalculateSummaryRequestDto dto)
    {
        _logger.LogInformation("Recalculating summary for employee {EmployeeId} {Year}-{Month}", dto.EmployeeId, dto.Year, dto.Month);

        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null || employee.IsDeleted)
        {
            return new ApiResponseDto { Success = false, Message = "Employee not found." };
        }

        await RecalculateSummaryAsync(dto.EmployeeId, dto.Year, dto.Month);

        return new ApiResponseDto { Success = true, Message = "Summary recalculated successfully." };
    }

    private async Task RecalculateSummaryAsync(int employeeId, int year, int month)
    {
        var records = await _context.AttendanceRecords
            .Where(a => a.EmployeeId == employeeId &&
                       a.WorkDate.Year == year &&
                       a.WorkDate.Month == month &&
                       !a.IsDeleted)
            .ToListAsync();

        // Get time off records for this month
        var timeOffRecords = await _context.TimeOffRecords
            .Where(t => t.EmployeeId == employeeId &&
                       t.TimeOffDate.Year == year &&
                       t.TimeOffDate.Month == month &&
                       !t.IsDeleted)
            .ToListAsync();

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return;

        var policy = await _context.AttendancePolicies
            .FirstOrDefaultAsync(p => p.IsActive && !p.IsDeleted);

        if (policy == null)
        {
            policy = new AttendancePolicy
            {
                WorkdayStart = new TimeOnly(7, 0),
                WorkdayEnd = new TimeOnly(14, 0),
                MonthlyTimeOffAllowance = 420,
                NinetyMinutesAllowance = 90,
                IsActive = true
            };
            _context.AttendancePolicies.Add(policy);
            await _context.SaveChangesAsync();
        }

        var summary = await _context.MonthlySummaries
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Year == year && s.Month == month && !s.IsDeleted);

        if (summary == null)
        {
            summary = new MonthlySummary
            {
                EmployeeId = employeeId,
                Year = year,
                Month = month
            };
            _context.MonthlySummaries.Add(summary);
        }

        // Calculate total time off used from both attendance records and time off records
        var timeOffFromAttendance = records.Sum(r => r.TimeOffMinutesUsed);
        var timeOffFromRecords = timeOffRecords.Sum(t => t.MinutesUsed);
        summary.TotalTimeOffMinutesUsed = timeOffFromAttendance + timeOffFromRecords;
        
        summary.TotalDelayMinutes = records.Sum(r => r.DelayMinutes);
        summary.NinetyMinutesConsumed = records.Sum(r => r.NinetyMinutesDeducted);
        summary.TotalOvertimeMinutes = records.Sum(r => r.OvertimeMinutes);
        summary.RemainingTimeOffMinutes = policy.MonthlyTimeOffAllowance - summary.TotalTimeOffMinutesUsed;
        summary.RemainingNinetyMinutes = policy.NinetyMinutesAllowance - summary.NinetyMinutesConsumed;
        summary.CalculatedAt = DateTime.UtcNow;
        summary.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
