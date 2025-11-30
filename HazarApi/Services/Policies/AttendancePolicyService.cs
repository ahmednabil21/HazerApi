using HazarApi.Data;
using HazarApi.DTO.Policies;
using HazarApi.Entities;
using HazarApi.IServices.Policies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HazarApi.Services.Policies;

public class AttendancePolicyService : IAttendancePolicyService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AttendancePolicyService> _logger;
    private readonly AutoMapper.IMapper _mapper;

    public AttendancePolicyService(AppDbContext context, ILogger<AttendancePolicyService> logger, AutoMapper.IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<AttendancePolicyDto> GetActivePolicyAsync()
    {
        _logger.LogInformation("Retrieving active attendance policy");

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

        return _mapper.Map<AttendancePolicyDto>(policy);
    }

    public async Task<AttendancePolicyDto> UpdateAsync(AttendancePolicyDto dto)
    {
        _logger.LogInformation("Updating attendance policy");

        var policy = await _context.AttendancePolicies
            .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted);

        if (policy == null)
        {
            throw new KeyNotFoundException("Policy not found.");
        }

        // Deactivate all other policies if this one is being activated
        if (dto.IsActive)
        {
            var otherPolicies = await _context.AttendancePolicies
                .Where(p => p.Id != dto.Id && p.IsActive && !p.IsDeleted)
                .ToListAsync();

            foreach (var otherPolicy in otherPolicies)
            {
                otherPolicy.IsActive = false;
                otherPolicy.UpdatedAt = DateTime.UtcNow;
            }
        }

        policy.WorkdayStart = dto.WorkdayStart;
        policy.WorkdayEnd = dto.WorkdayEnd;
        policy.MonthlyTimeOffAllowance = dto.MonthlyTimeOffAllowance;
        policy.NinetyMinutesAllowance = dto.NinetyMinutesAllowance;
        policy.IsActive = dto.IsActive;
        policy.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return _mapper.Map<AttendancePolicyDto>(policy);
    }
}
