using HazarApi.DTO.Dashboard;

namespace HazarApi.IServices.Dashboard;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(int year, int month);
    Task<IReadOnlyCollection<TopEmployeeDto>> GetTopDelaysAsync(int year, int month, int take = 5);
    Task<IReadOnlyCollection<TopEmployeeDto>> GetTopCommitmentAsync(int year, int month, int take = 5);
}

