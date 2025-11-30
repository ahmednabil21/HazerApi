using AutoMapper;
using HazarApi.DTO.Auth;
using HazarApi.DTO.Attendance;
using HazarApi.DTO.Employees;
using HazarApi.DTO.Policies;
using HazarApi.DTO.Roles;
using HazarApi.DTO.Summary;
using HazarApi.Entities;

namespace HazarApi.Mapper;

/// <summary>
/// ملف تعريف AutoMapper الرئيسي لتوصيل الكيانات مع الـ DTOs.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        ConfigureEmployeeMaps();
        ConfigureAttendanceMaps();
        ConfigureSummaryMaps();
        ConfigurePolicyMaps();
        ConfigureRoleMaps();
        ConfigureSessionMaps();
    }

    private void ConfigureEmployeeMaps()
    {
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.Role,
                opt => opt.MapFrom(src => src.Role != null ? src.Role.Type.ToString() : RoleType.Employee.ToString()));

        CreateMap<EmployeeCreateDto, Employee>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.RoleId, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore());

        CreateMap<EmployeeUpdateDto, Employee>()
            .ForMember(dest => dest.RoleId, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
    }

    private void ConfigureAttendanceMaps()
    {
        CreateMap<AttendanceCreateDto, AttendanceRecord>()
            .ForMember(dest => dest.CheckOut,
                opt => opt.MapFrom(src => src.CheckOut ?? new TimeOnly(14, 0)))
            .ForMember(dest => dest.TimeOffMinutesUsed,
                opt => opt.MapFrom(src => src.TimeOffMinutesUsed ?? 0))
            .ForMember(dest => dest.DelayMinutes, opt => opt.Ignore())
            .ForMember(dest => dest.OvertimeMinutes, opt => opt.Ignore())
            .ForMember(dest => dest.NinetyMinutesDeducted, opt => opt.Ignore())
            .ForMember(dest => dest.IsLocked, opt => opt.Ignore());

        CreateMap<AttendanceUpdateDto, AttendanceRecord>()
            .ForMember(dest => dest.CheckOut,
                opt => opt.MapFrom(src => src.CheckOut ?? new TimeOnly(14, 0)))
            .ForMember(dest => dest.TimeOffMinutesUsed,
                opt => opt.MapFrom(src => src.TimeOffMinutesUsed ?? 0))
            .ForMember(dest => dest.DelayMinutes, opt => opt.Ignore())
            .ForMember(dest => dest.OvertimeMinutes, opt => opt.Ignore())
            .ForMember(dest => dest.NinetyMinutesDeducted, opt => opt.Ignore())
            .ForMember(dest => dest.IsLocked, opt => opt.Ignore());

        CreateMap<AttendanceRecord, AttendanceRecordDto>();
    }

    private void ConfigureSummaryMaps()
    {
        CreateMap<MonthlySummary, MonthlySummaryDto>()
            .ForMember(dest => dest.EmployeeName,
                opt => opt.MapFrom(src => src.Employee != null ? src.Employee.FullName : string.Empty));

        CreateMap<RecalculateSummaryRequestDto, MonthlySummary>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }

    private void ConfigurePolicyMaps()
    {
        CreateMap<AttendancePolicy, AttendancePolicyDto>().ReverseMap()
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
    }

    private void ConfigureRoleMaps()
    {
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Type.ToString()));
    }

    private void ConfigureSessionMaps()
    {
        CreateMap<UserSession, UserSessionDto>()
            .ForMember(dest => dest.EmployeeName,
                opt => opt.MapFrom(src => src.Employee != null ? src.Employee.FullName : string.Empty));
    }
}

