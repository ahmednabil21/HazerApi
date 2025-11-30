using HazarApi.Data;
using HazarApi.DTO.Attendance;
using HazarApi.DTO.Common;
using HazarApi.Entities;
using HazarApi.IServices.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HazarApi.Services.Attendance;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AttendanceService> _logger;
    private readonly AutoMapper.IMapper _mapper;

    public AttendanceService(AppDbContext context, ILogger<AttendanceService> logger, AutoMapper.IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<AttendanceRecordDto> CreateAsync(int employeeId, AttendanceCreateDto dto)
    {
        _logger.LogInformation("Creating attendance for employee {EmployeeId} on {Date}", employeeId, dto.WorkDate);

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || employee.IsDeleted || !employee.IsActive)
        {
            throw new KeyNotFoundException("Employee not found or inactive.");
        }

        // Check if Friday or Saturday (weekend)
        if (dto.WorkDate.DayOfWeek == DayOfWeek.Friday || dto.WorkDate.DayOfWeek == DayOfWeek.Saturday)
        {
            throw new InvalidOperationException("Cannot record attendance on Friday or Saturday (weekend).");
        }

        // Check if record already exists
        if (await _context.AttendanceRecords.AnyAsync(a => a.EmployeeId == employeeId && a.WorkDate == dto.WorkDate && !a.IsDeleted))
        {
            throw new InvalidOperationException("Attendance record already exists for this date.");
        }

        var policy = await GetActivePolicyAsync();
        var checkOut = dto.CheckOut.HasValue ? dto.CheckOut.Value : policy.WorkdayEnd;

        var calculations = CalculateAttendanceMetrics(
            dto.CheckIn,
            checkOut,
            dto.TimeOffMinutesUsed ?? 0,
            policy,
            employee.NinetyMinutesBalance
        );

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            WorkDate = dto.WorkDate,
            CheckIn = dto.CheckIn,
            CheckOut = checkOut,
            TimeOffMinutesUsed = calculations.TimeOffUsed,
            DelayMinutes = calculations.DelayMinutes,
            OvertimeMinutes = calculations.OvertimeMinutes,
            NinetyMinutesDeducted = calculations.NinetyMinutesDeducted,
            Notes = dto.Notes,
            IsLocked = false
        };

        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync();

        // Update employee's 90 minutes balance
        employee.NinetyMinutesBalance -= calculations.NinetyMinutesDeducted;
        await _context.SaveChangesAsync();

        // Update monthly summary
        await UpdateMonthlySummaryAsync(employeeId, dto.WorkDate.Year, dto.WorkDate.Month);

        return _mapper.Map<AttendanceRecordDto>(record);
    }

    public async Task<AttendanceRecordDto> UpdateAsync(int employeeId, int attendanceId, AttendanceUpdateDto dto)
    {
        _logger.LogInformation("Updating attendance {AttendanceId} for employee {EmployeeId}", attendanceId, employeeId);

        var record = await _context.AttendanceRecords
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == attendanceId && a.EmployeeId == employeeId && !a.IsDeleted);

        if (record == null)
        {
            throw new KeyNotFoundException("Attendance record not found.");
        }

        // Only allow editing on the same day
        if (record.WorkDate != DateOnly.FromDateTime(DateTime.Now))
        {
            throw new InvalidOperationException("Can only edit attendance records for today.");
        }

        if (record.IsLocked)
        {
            throw new InvalidOperationException("Attendance record is locked and cannot be modified.");
        }

        var policy = await GetActivePolicyAsync();
        var checkOut = dto.CheckOut.HasValue ? dto.CheckOut.Value : record.CheckOut;

        var checkIn = dto.CheckIn;
        var timeOffUsed = dto.TimeOffMinutesUsed ?? record.TimeOffMinutesUsed;

        var calculations = CalculateAttendanceMetrics(
            checkIn,
            checkOut,
            timeOffUsed,
            policy,
            record.Employee!.NinetyMinutesBalance
        );

        record.CheckIn = checkIn;
        record.CheckOut = checkOut;
        record.TimeOffMinutesUsed = calculations.TimeOffUsed;
        record.DelayMinutes = calculations.DelayMinutes;
        record.OvertimeMinutes = calculations.OvertimeMinutes;
        record.NinetyMinutesDeducted = calculations.NinetyMinutesDeducted;
        if (dto.Notes != null)
            record.Notes = dto.Notes;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Update monthly summary
        await UpdateMonthlySummaryAsync(employeeId, record.WorkDate.Year, record.WorkDate.Month);

        return _mapper.Map<AttendanceRecordDto>(record);
    }

    public async Task<IReadOnlyCollection<AttendanceRecordDto>> GetMonthlyAsync(int employeeId, int year, int month, bool includeLocked = false)
    {
        _logger.LogInformation("Fetching attendance for employee {EmployeeId} year {Year} month {Month}", employeeId, year, month);

        var query = _context.AttendanceRecords
            .Where(a => a.EmployeeId == employeeId &&
                       a.WorkDate.Year == year &&
                       a.WorkDate.Month == month &&
                       !a.IsDeleted);

        if (!includeLocked)
        {
            query = query.Where(a => !a.IsLocked);
        }

        var records = await query
            .OrderBy(a => a.WorkDate)
            .ToListAsync();

        return _mapper.Map<List<AttendanceRecordDto>>(records);
    }

    private async Task<AttendancePolicy> GetActivePolicyAsync()
    {
        var policy = await _context.AttendancePolicies
            .FirstOrDefaultAsync(p => p.IsActive && !p.IsDeleted);

        if (policy == null)
        {
            // Create default policy if none exists
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

        return policy;
    }

    private (int DelayMinutes, int OvertimeMinutes, int TimeOffUsed, int NinetyMinutesDeducted) CalculateAttendanceMetrics(
        TimeOnly checkIn,
        TimeOnly checkOut,
        int timeOffMinutesUsed,
        AttendancePolicy policy,
        int currentNinetyMinutesBalance)
    {
        // Validate time off (max 4 hours = 240 minutes)
        var timeOffUsed = Math.Min(timeOffMinutesUsed, 240);

        // Calculate delay
        var checkInMinutes = checkIn.Hour * 60 + checkIn.Minute;
        var workStartMinutes = policy.WorkdayStart.Hour * 60 + policy.WorkdayStart.Minute;
        var delayMinutes = Math.Max(0, checkInMinutes - workStartMinutes - timeOffUsed);

        // Calculate overtime
        var checkOutMinutes = checkOut.Hour * 60 + checkOut.Minute;
        var workEndMinutes = policy.WorkdayEnd.Hour * 60 + policy.WorkdayEnd.Minute;
        var overtimeMinutes = Math.Max(0, checkOutMinutes - workEndMinutes);

        // Deduct from 90 minutes balance
        var ninetyMinutesDeducted = 0;
        if (delayMinutes > 0 && currentNinetyMinutesBalance > 0)
        {
            ninetyMinutesDeducted = Math.Min(delayMinutes, currentNinetyMinutesBalance);
            delayMinutes -= ninetyMinutesDeducted;
        }

        return (delayMinutes, overtimeMinutes, timeOffUsed, ninetyMinutesDeducted);
    }

    private async Task UpdateMonthlySummaryAsync(int employeeId, int year, int month)
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

        var policy = await GetActivePolicyAsync();

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

    public async Task<AttendanceRecordDto> CheckInAsync(int employeeId, CheckInDto dto)
    {
        _logger.LogInformation("Check-in for employee {EmployeeId} on {Date} at {Time}", employeeId, dto.WorkDate, dto.CheckInTime);

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || employee.IsDeleted || !employee.IsActive)
        {
            throw new KeyNotFoundException("Employee not found or inactive.");
        }

        // Check if Friday or Saturday (weekend)
        if (dto.WorkDate.DayOfWeek == DayOfWeek.Friday || dto.WorkDate.DayOfWeek == DayOfWeek.Saturday)
        {
            throw new InvalidOperationException("Cannot record attendance on Friday or Saturday (weekend).");
        }

        // Check if record already exists
        var existingRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == dto.WorkDate && !a.IsDeleted);

        if (existingRecord != null)
        {
            throw new InvalidOperationException("Attendance record already exists for this date. Please use update instead.");
        }

        var policy = await GetActivePolicyAsync();

        // Create attendance record with check-in only
        // Check-out will be set to default workday end for now, will be updated on check-out
        var checkOut = policy.WorkdayEnd;

        var calculations = CalculateAttendanceMetrics(
            dto.CheckInTime,
            checkOut,
            dto.TimeOffMinutesUsed ?? 0,
            policy,
            employee.NinetyMinutesBalance
        );

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            WorkDate = dto.WorkDate,
            CheckIn = dto.CheckInTime,
            CheckOut = checkOut, // Default, will be updated on check-out
            TimeOffMinutesUsed = calculations.TimeOffUsed,
            DelayMinutes = calculations.DelayMinutes,
            OvertimeMinutes = 0, // Will be calculated on check-out
            NinetyMinutesDeducted = calculations.NinetyMinutesDeducted,
            Notes = dto.Notes,
            IsLocked = false
        };

        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync();

        // Update employee's 90 minutes balance
        employee.NinetyMinutesBalance -= calculations.NinetyMinutesDeducted;
        await _context.SaveChangesAsync();

        // Update monthly summary
        await UpdateMonthlySummaryAsync(employeeId, dto.WorkDate.Year, dto.WorkDate.Month);

        return _mapper.Map<AttendanceRecordDto>(record);
    }

    public async Task<AttendanceRecordDto> CheckOutAsync(int employeeId, CheckOutDto dto)
    {
        _logger.LogInformation("Check-out for employee {EmployeeId} on {Date} at {Time}", employeeId, dto.WorkDate, dto.CheckOutTime);

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || employee.IsDeleted || !employee.IsActive)
        {
            throw new KeyNotFoundException("Employee not found or inactive.");
        }

        // Find existing attendance record for this date
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == dto.WorkDate && !a.IsDeleted);

        if (record == null)
        {
            throw new KeyNotFoundException("No check-in record found for this date. Please check in first.");
        }

        if (record.IsLocked)
        {
            throw new InvalidOperationException("Attendance record is locked and cannot be modified.");
        }

        var policy = await GetActivePolicyAsync();

        // Recalculate with actual check-out time
        var calculations = CalculateAttendanceMetrics(
            record.CheckIn,
            dto.CheckOutTime,
            record.TimeOffMinutesUsed,
            policy,
            employee.NinetyMinutesBalance
        );

        // Update record with check-out time and recalculated values
        record.CheckOut = dto.CheckOutTime;
        record.DelayMinutes = calculations.DelayMinutes;
        record.OvertimeMinutes = calculations.OvertimeMinutes;
        record.NinetyMinutesDeducted = calculations.NinetyMinutesDeducted;
        if (dto.Notes != null)
            record.Notes = dto.Notes;
        record.UpdatedAt = DateTime.UtcNow;

        // Close all active sessions for this employee when checking out
        var activeSessions = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId && s.IsActive && !s.IsDeleted)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.LogoutTime = DateTime.UtcNow;
            session.IsActive = false;
            session.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Update monthly summary
        await UpdateMonthlySummaryAsync(employeeId, dto.WorkDate.Year, dto.WorkDate.Month);

        return _mapper.Map<AttendanceRecordDto>(record);
    }

    public async Task<ApiResponseDto> AddTimeOffAsync(int employeeId, AddTimeOffDto dto)
    {
        _logger.LogInformation("Adding time off for employee {EmployeeId} on {Date}, {Minutes} minutes", employeeId, dto.TimeOffDate, dto.MinutesUsed);

        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null || employee.IsDeleted || !employee.IsActive)
        {
            return new ApiResponseDto { Success = false, Message = "Employee not found or inactive." };
        }

        // Check if Friday or Saturday (weekend)
        if (dto.TimeOffDate.DayOfWeek == DayOfWeek.Friday || dto.TimeOffDate.DayOfWeek == DayOfWeek.Saturday)
        {
            return new ApiResponseDto { Success = false, Message = "Cannot add time off on Friday or Saturday (weekend)." };
        }

        // Validate minutes (max 4 hours = 240 minutes per day)
        if (dto.MinutesUsed > 240)
        {
            return new ApiResponseDto { Success = false, Message = "Time off cannot exceed 4 hours (240 minutes) per day." };
        }

        // Get monthly summary to check available balance
        var year = dto.TimeOffDate.Year;
        var month = dto.TimeOffDate.Month;
        var summary = await _context.MonthlySummaries
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Year == year && s.Month == month && !s.IsDeleted);

        var policy = await GetActivePolicyAsync();
        var availableBalance = summary?.RemainingTimeOffMinutes ?? policy.MonthlyTimeOffAllowance;

        if (dto.MinutesUsed > availableBalance)
        {
            return new ApiResponseDto { Success = false, Message = $"Insufficient time off balance. Available: {availableBalance} minutes, Requested: {dto.MinutesUsed} minutes." };
        }

        // Check if there's an attendance record for this date
        var attendanceRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == dto.TimeOffDate && !a.IsDeleted);

        var isUsedForDelay = false;
        if (attendanceRecord != null && attendanceRecord.DelayMinutes > 0)
        {
            // If time off covers the delay, mark it as used for delay
            if (dto.MinutesUsed >= attendanceRecord.DelayMinutes)
            {
                isUsedForDelay = true;
                // Adjust the delay: if time off covers delay, reduce delay and adjust 90 minutes deduction
                var originalDelay = attendanceRecord.DelayMinutes;
                var originalNinetyDeducted = attendanceRecord.NinetyMinutesDeducted;
                
                // If delay was covered by 90 minutes, we need to restore that and use time off instead
                if (originalNinetyDeducted > 0)
                {
                    // Restore 90 minutes balance
                    employee.NinetyMinutesBalance += originalNinetyDeducted;
                    attendanceRecord.NinetyMinutesDeducted = 0;
                }
                
                attendanceRecord.DelayMinutes = 0;
                attendanceRecord.TimeOffMinutesUsed += dto.MinutesUsed;
                attendanceRecord.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Partial coverage
                attendanceRecord.TimeOffMinutesUsed += dto.MinutesUsed;
                var remainingDelay = attendanceRecord.DelayMinutes - dto.MinutesUsed;
                
                // Recalculate 90 minutes deduction for remaining delay
                if (remainingDelay > 0 && employee.NinetyMinutesBalance > 0)
                {
                    var ninetyToDeduct = Math.Min(remainingDelay, employee.NinetyMinutesBalance);
                    attendanceRecord.NinetyMinutesDeducted = ninetyToDeduct;
                    attendanceRecord.DelayMinutes = remainingDelay - ninetyToDeduct;
                    employee.NinetyMinutesBalance -= ninetyToDeduct;
                }
                else
                {
                    attendanceRecord.DelayMinutes = remainingDelay;
                }
                attendanceRecord.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Create time off record
        var timeOffRecord = new TimeOffRecord
        {
            EmployeeId = employeeId,
            TimeOffDate = dto.TimeOffDate,
            MinutesUsed = dto.MinutesUsed,
            Reason = dto.Reason,
            IsUsedForDelay = isUsedForDelay
        };

        _context.TimeOffRecords.Add(timeOffRecord);
        await _context.SaveChangesAsync();

        // Update monthly summary
        await UpdateMonthlySummaryAsync(employeeId, year, month);

        return new ApiResponseDto
        {
            Success = true,
            Message = $"Time off added successfully. {dto.MinutesUsed} minutes deducted from monthly balance."
        };
    }
}
