using HazarApi.Data;
using HazarApi.DTO.Common;
using HazarApi.DTO.Employees;
using HazarApi.Entities;
using HazarApi.IServices.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace HazarApi.Services.Employees;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _context;
    private readonly ILogger<EmployeeService> _logger;
    private readonly AutoMapper.IMapper _mapper;

    public EmployeeService(AppDbContext context, ILogger<EmployeeService> logger, AutoMapper.IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<EmployeeDto>> GetAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? includeInactive = null)
    {
        _logger.LogInformation("Fetching employees page {PageNumber}", pageNumber);

        var query = _context.Employees
            .Include(e => e.Role)
            .Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e => e.FullName.Contains(searchTerm) || e.Username.Contains(searchTerm));
        }

        if (includeInactive.HasValue && !includeInactive.Value)
        {
            query = query.Where(e => e.IsActive);
        }

        var totalCount = await query.CountAsync();
        var employees = await query
            .OrderBy(e => e.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var employeeDtos = _mapper.Map<List<EmployeeDto>>(employees);

        return new PagedResultDto<EmployeeDto>
        {
            Items = employeeDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching employee {EmployeeId}", id);

        var employee = await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        return employee == null ? null : _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<EmployeeDto> CreateAsync(EmployeeCreateDto dto)
    {
        _logger.LogInformation("Creating employee {Username}", dto.Username);

        if (await _context.Employees.AnyAsync(e => e.Username == dto.Username && !e.IsDeleted))
        {
            throw new InvalidOperationException($"Username '{dto.Username}' already exists.");
        }

        var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.Type == RoleType.Employee);
        if (employeeRole == null)
        {
            employeeRole = new Role { Type = RoleType.Employee, Description = "Employee role" };
            _context.Roles.Add(employeeRole);
            await _context.SaveChangesAsync();
        }

        var employee = new Employee
        {
            FullName = dto.FullName,
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            JobTitle = dto.JobTitle,
            IsActive = true,
            RoleId = employeeRole.Id,
            MonthlyTimeOffBalance = 420,
            NinetyMinutesBalance = 90
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        await _context.Entry(employee).Reference(e => e.Role).LoadAsync();

        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, EmployeeUpdateDto dto)
    {
        _logger.LogInformation("Updating employee {EmployeeId}", id);

        var employee = await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found.");
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
            employee.FullName = dto.FullName;

        if (dto.JobTitle != null)
            employee.JobTitle = dto.JobTitle;

        employee.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _mapper.Map<EmployeeDto>(employee);
    }

    public async Task<ApiResponseDto> ToggleStatusAsync(int id, bool isActive)
    {
        _logger.LogInformation("Toggling employee {EmployeeId} active={IsActive}", id, isActive);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || employee.IsDeleted)
        {
            return new ApiResponseDto { Success = false, Message = "Employee not found." };
        }

        employee.IsActive = isActive;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponseDto { Success = true, Message = $"Employee {(isActive ? "activated" : "deactivated")} successfully." };
    }

    public async Task<ApiResponseDto> ResetPasswordAsync(int id, ResetPasswordDto dto)
    {
        _logger.LogInformation("Resetting password for employee {EmployeeId}", id);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || employee.IsDeleted)
        {
            return new ApiResponseDto { Success = false, Message = "Employee not found." };
        }

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponseDto { Success = true, Message = "Password reset successfully." };
    }

    public async Task<ApiResponseDto> SoftDeleteAsync(int id)
    {
        _logger.LogInformation("Soft deleting employee {EmployeeId}", id);

        var employee = await _context.Employees.FindAsync(id);
        if (employee == null || employee.IsDeleted)
        {
            return new ApiResponseDto { Success = false, Message = "Employee not found." };
        }

        employee.IsDeleted = true;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponseDto { Success = true, Message = "Employee deleted successfully." };
    }

    public async Task<EmployeeBalanceDto> GetMyBalanceAsync(int employeeId)
    {
        _logger.LogInformation("Fetching balance for employee {EmployeeId}", employeeId);

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.IsDeleted);

        if (employee == null)
        {
            throw new KeyNotFoundException("Employee not found.");
        }

        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        var summary = await _context.MonthlySummaries
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.Year == year && s.Month == month && !s.IsDeleted);

        var policy = await _context.AttendancePolicies
            .FirstOrDefaultAsync(p => p.IsActive && !p.IsDeleted);

        var monthlyAllowance = policy?.MonthlyTimeOffAllowance ?? 420;
        var totalUsed = summary?.TotalTimeOffMinutesUsed ?? 0;
        var remaining = summary?.RemainingTimeOffMinutes ?? monthlyAllowance;

        return new EmployeeBalanceDto
        {
            EmployeeId = employee.Id,
            EmployeeName = employee.FullName,
            MonthlyTimeOffBalance = remaining, // الرصيد المتبقي من الزمنيات
            NinetyMinutesBalance = employee.NinetyMinutesBalance,
            Year = year,
            Month = month,
            TotalTimeOffUsed = totalUsed,
            RemainingTimeOffMinutes = remaining
        };
    }
}
