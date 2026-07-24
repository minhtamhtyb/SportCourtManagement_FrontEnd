using Microsoft.AspNetCore.Http;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Auth;

namespace SportCourtManagement_FrontEnd.Services.Interfaces;

public interface IAuthService
{
    Task<AuthLoginResult> LoginAsync(LoginRequest request);
    Task RegisterAsync(RegisterRequest request);
    Task VerifyEmailAsync(VerifyEmailRequest request);
    Task LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<UserDto> UpdateProfileAsync(UpdateProfileViewModel request);
    Task ChangePasswordAsync(ChangePasswordViewModel request);
    Task<AuthLoginResult> GoogleLoginAsync(string idToken);
}

public interface ICourtService
{
    Task<ComplexStatsDto> GetStatsAsync();
    Task<PagedResult<CourtComplexDto>> GetComplexesAsync(
        string? search,
        int? courtTypeId,
        int page,
        int pageSize
    );
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
    Task<CourtLifecycleResultDto> DeactivateCourtAsync(int id);
    Task<CourtLifecycleResultDto> RestoreCourtAsync(int id);
    Task<MaintenanceConflictPreviewDto> PreviewMaintenanceConflictsAsync(int courtId, DateTime start, DateTime end);
    Task<CourtLifecycleResultDto> ScheduleMaintenanceAsync(int courtId, ScheduleCourtMaintenanceRequest request);
}

public interface IServiceCatalogService
{
    Task<List<ServiceDto>> GetServicesAsync(string? category, string? search);
    Task<ServiceDto?> GetServiceByIdAsync(int id);
    Task<ServiceDto> CreateServiceAsync(ServiceDto dto);
    Task UpdateServiceAsync(int id, ServiceDto dto);
    Task DeleteServiceAsync(int id);
}

public interface IComplexServiceOfferingService
{
    Task<List<ComplexCourtTypeServiceDto>> GetByComplexAsync(int complexId);
    Task<List<ComplexCourtTypeServiceDto>> GetByComplexAndCourtTypeAsync(
        int complexId,
        int courtTypeId
    );
    Task<ComplexCourtTypeServiceDto> CreateAsync(
        int complexId,
        int courtTypeId,
        ComplexCourtTypeServiceDto dto
    );
    Task UpdateAsync(int offeringId, ComplexCourtTypeServiceDto dto);
    Task DeleteAsync(int offeringId);
}

public interface IReportService
{
    Task<DashboardSummaryDto> GetDashboardAsync();
    Task<AdminDashboardDto> GetAdminDashboardAsync();
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
    Task UpdateUserAccessAsync(int id, string role, bool isActive);
    Task<UserDto> CreateUserAsync(UserDto dto, string password);
    Task UpdateUserByAdminAsync(int id, UserDto dto);
    Task DeleteUserAsync(int id);
}

public interface IRoleService
{
    Task<List<RoleDto>> GetRolesAsync();
    Task<List<PermissionMatrixRow>> GetPermissionMatrixAsync();
    Task UpdatePermissionMatrixAsync(List<PermissionMatrixRow> matrix);
}
