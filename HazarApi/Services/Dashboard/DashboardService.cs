using HazarApi.Data;
using HazarApi.DTO.Dashboard;
using HazarApi.IServices.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HazarApi.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(AppDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(int year, int month)
    {
        _logger.LogInformation("Fetching dashboard stats for {Year}-{Month}", year, month);

        var totalEmployees = await _context.Employees.CountAsync(e => !e.IsDeleted && e.IsActive);

        var summaries = await _context.MonthlySummaries
            .Where(s => s.Year == year && s.Month == month && !s.IsDeleted)
            .Include(s => s.Employee)
            .Where(s => !s.Employee!.IsDeleted && s.Employee.IsActive)
            .ToListAsync();

        var totalDelay = summaries.Sum(s => s.TotalDelayMinutes);
        var totalTimeOff = summaries.Sum(s => s.TotalTimeOffMinutesUsed);
        var totalOvertime = summaries.Sum(s => s.TotalOvertimeMinutes);

        return new DashboardStatsDto
        {
            TotalEmployees = totalEmployees,
            TotalDelayMinutes = totalDelay,
            TotalTimeOffMinutes = totalTimeOff,
            TotalOvertimeMinutes = totalOvertime,
            Year = year,
            Month = month
        };
    }

    public async Task<IReadOnlyCollection<TopEmployeeDto>> GetTopDelaysAsync(int year, int month, int take = 5)
    {
        _logger.LogInformation("Fetching top delays for {Year}-{Month}", year, month);

        var topDelays = await _context.MonthlySummaries
            .Where(s => s.Year == year && s.Month == month && !s.IsDeleted)
            .Include(s => s.Employee)
            .Where(s => !s.Employee!.IsDeleted && s.Employee.IsActive)
            .OrderByDescending(s => s.TotalDelayMinutes)
            .Take(take)
            .Select(s => new TopEmployeeDto
            {
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee!.FullName,
                Minutes = s.TotalDelayMinutes,
                Value = s.TotalDelayMinutes,
                Metric = "DelayMinutes"
            })
            .ToListAsync();

        return topDelays;
    }

    public async Task<IReadOnlyCollection<TopEmployeeDto>> GetTopCommitmentAsync(int year, int month, int take = 5)
    {
        _logger.LogInformation("Fetching top commitment for {Year}-{Month}", year, month);

        // Top commitment = least delays, most overtime, least time off used
        var topCommitment = await _context.MonthlySummaries
            .Where(s => s.Year == year && s.Month == month && !s.IsDeleted)
            .Include(s => s.Employee)
            .Where(s => !s.Employee!.IsDeleted && s.Employee.IsActive)
            .OrderBy(s => s.TotalDelayMinutes)
            .ThenByDescending(s => s.TotalOvertimeMinutes)
            .ThenBy(s => s.TotalTimeOffMinutesUsed)
            .Take(take)
            .Select(s => new TopEmployeeDto
            {
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee!.FullName,
                Minutes = s.TotalOvertimeMinutes - s.TotalDelayMinutes, // Commitment score
                Value = s.TotalOvertimeMinutes - s.TotalDelayMinutes,
                Metric = "CommitmentScore"
            })
            .ToListAsync();

        return topCommitment;
    }
}
