using Microsoft.AspNetCore.Http;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.DTOs;

namespace SportCourtManagement_FrontEnd.Services.Interfaces;

public interface IAuthService
{
    Task<AuthLoginResult> LoginAsync(LoginRequest request);
    Task RegisterAsync(RegisterRequest request);
    Task VerifyEmailAsync(VerifyEmailRequest request);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
}

public interface ICourtService
{
    Task<ComplexStatsDto> GetStatsAsync();
    Task<PagedResult<CourtComplexDto>> GetComplexesAsync(string? search, int? courtTypeId, int page, int pageSize);
    Task<CourtComplexDto?> GetComplexByIdAsync(int id);
    Task<CourtComplexDto> CreateComplexAsync(CourtComplexDto dto);
    Task UpdateComplexAsync(int id, CourtComplexDto dto);
    Task DeleteComplexAsync(int id);
    Task<string> UploadComplexImageAsync(IFormFile file);
    Task<List<CourtTypeDto>> GetCourtTypesAsync();
    Task<List<UserDto>> GetManagersAsync();
    Task<UserDto?> GetManagerByIdAsync(int id);
    Task<List<CourtDto>> GetCourtsByComplexAsync(int complexId, string? search, string? status);
    Task<CourtDto?> GetCourtByIdAsync(int id);
    Task<CourtDto> CreateCourtAsync(CourtDto dto);
    Task UpdateCourtAsync(int id, CourtDto dto);
    Task UpdateCourtStatusAsync(int id, string status);
    Task DeleteCourtAsync(int id);
}

public interface IServiceCatalogService
{
    Task<List<ServiceDto>> GetServicesAsync(string? category, string? search);
    Task<ServiceDto?> GetServiceByIdAsync(int id);
    Task<ServiceDto> CreateServiceAsync(ServiceDto dto);
    Task UpdateServiceAsync(int id, ServiceDto dto);
    Task DeleteServiceAsync(int id);
}

public interface IReportService
{
    Task<DashboardSummaryDto> GetDashboardAsync();
    Task<List<RevenueDataPointDto>> GetRevenueReportAsync(string period);
    Task<List<CourtUsageDto>> GetCourtUsageAsync();
    Task<ComplexStatsDto> GetComplexStatsAsync();
}

public interface IUserService
{
    Task<PagedResult<UserDto>> GetUsersAsync(string? search, string? role, int page, int pageSize);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task UpdateUserRoleAsync(int id, string role);
    Task ToggleUserStatusAsync(int id, bool isActive);
}

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync();
    Task<List<PermissionMatrixRow>> GetPermissionMatrixAsync();
}
