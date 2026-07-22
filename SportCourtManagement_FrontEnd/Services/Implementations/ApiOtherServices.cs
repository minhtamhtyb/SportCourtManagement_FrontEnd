using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Services.Api;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class ApiServiceCatalogService(ApiClient api) : IServiceCatalogService
{
    public async Task<List<ServiceDto>> GetServicesAsync(string? category, string? search)
    {
        var query = "api/services";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(category))
            parts.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(search))
            parts.Add($"search={Uri.EscapeDataString(search)}");
        
        parts.Add("activeOnly=false");

        if (parts.Count > 0)
            query += "?" + string.Join("&", parts);

        return await api.GetDataAsync<List<ServiceDto>>(query) ?? [];
    }

    public Task<ServiceDto?> GetServiceByIdAsync(int id) =>
        api.GetDataAsync<ServiceDto>($"api/services/{id}");

    public async Task<ServiceDto> CreateServiceAsync(ServiceDto dto)
    {
        var payload = new
        {
            dto.ServiceName,
            dto.Category,
            dto.Price,
            dto.Unit,
            dto.Description,
            dto.StockQty,
            dto.IsActive
        };
        return await api.PostDataAsync<ServiceDto>("api/services", payload)
            ?? throw new InvalidOperationException("Tạo dịch vụ thất bại.");
    }

    public async Task UpdateServiceAsync(int id, ServiceDto dto)
    {
        var payload = new
        {
            dto.ServiceName,
            dto.Category,
            dto.Price,
            dto.Unit,
            dto.Description,
            dto.StockQty,
            dto.IsActive
        };
        await api.PutDataAsync<ServiceDto>($"api/services/{id}", payload);
    }

    public Task DeleteServiceAsync(int id) =>
        api.DeleteAsync($"api/services/{id}");
}

public class ApiReportService(ApiClient api) : IReportService
{
    public async Task<DashboardSummaryDto> GetDashboardAsync() =>
        await api.GetDataAsync<DashboardSummaryDto>("api/reports/dashboard")
            ?? new DashboardSummaryDto();

    public async Task<AdminDashboardDto> GetAdminDashboardAsync() =>
        await api.GetDataAsync<AdminDashboardDto>("api/staff-dashboard/admin-dashboard")
            ?? new AdminDashboardDto();

    public async Task<List<RevenueDataPointDto>> GetRevenueReportAsync(string period)
    {
        var data = await api.GetDataAsync<List<RevenueDataPointDto>>(
            $"api/reports/revenue?period={Uri.EscapeDataString(period)}");
        return data ?? [];
    }

    public async Task<List<CourtUsageDto>> GetCourtUsageAsync()
    {
        var data = await api.GetDataAsync<List<CourtUsageDto>>("api/reports/court-usage");
        return data ?? [];
    }

    public Task<ComplexStatsDto> GetComplexStatsAsync()
    {
        var stats = api.GetDataAsync<ComplexStatsDto>("api/complexes/stats");
        return stats.ContinueWith(t => t.Result ?? new ComplexStatsDto());
    }
}

public class ApiUserService(ApiClient api) : IUserService
{
    public async Task<PagedResult<UserDto>> GetUsersAsync(string? search, string? role, int page, int pageSize)
    {
        var query = $"api/users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(role))
            query += $"&role={Uri.EscapeDataString(role)}";

        var result = await api.GetDataAsync<PagedUsersApi>(query);
        if (result?.Items == null)
            return new PagedResult<UserDto>();

        return new PagedResult<UserDto>
        {
            Items = result.Items.Select(MapUser).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasNextPage = result.Page < result.TotalPages,
            HasPreviousPage = result.Page > 1
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await api.GetDataAsync<UserSummaryApi>($"api/users/{id}");
        return user is null ? null : MapUser(user);
    }

    public async Task UpdateUserAccessAsync(int id, string role, bool isActive)
    {
        await api.PutDataAsync<UserSummaryApi>($"api/users/{id}/access", new { role, isActive });
    }

    public async Task UpdateUserRoleAsync(int id, string role)
    {
        var user = await GetUserByIdAsync(id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        await UpdateUserAccessAsync(id, role, user.IsActive);
    }

    public async Task ToggleUserStatusAsync(int id, bool isActive)
    {
        var user = await GetUserByIdAsync(id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        await UpdateUserAccessAsync(id, user.Role, isActive);
    }

    public async Task<UserDto> CreateUserAsync(UserDto dto, string password)
    {
        var payload = new
        {
            dto.FullName,
            dto.Email,
            dto.Phone,
            password,
            dto.Role,
            Gender = dto.Gender ?? "Other",
            SkillLevel = dto.SkillLevel ?? "Beginner",
            dto.IsActive
        };
        var res = await api.PostDataAsync<UserSummaryApi>("api/users", payload);
        return res != null ? MapUser(res) : throw new InvalidOperationException("Tạo tài khoản người dùng thất bại.");
    }

    public async Task UpdateUserByAdminAsync(int id, UserDto dto)
    {
        var payload = new
        {
            dto.FullName,
            dto.Email,
            dto.Phone,
            dto.Role,
            Gender = dto.Gender ?? "Other",
            SkillLevel = dto.SkillLevel ?? "Beginner",
            dto.IsActive
        };
        await api.PutDataAsync<UserSummaryApi>($"api/users/{id}", payload);
    }

    public async Task DeleteUserAsync(int id)
    {
        await api.DeleteAsync($"api/users/{id}");
    }

    private static UserDto MapUser(UserSummaryApi u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        AvatarUrl = u.AvatarUrl,
        Role = u.Role,
        MembershipTier = u.MembershipTierName,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        Gender = u.Gender,
        SkillLevel = u.SkillLevel
    };

    private class PagedUsersApi
    {
        public List<UserSummaryApi> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    private class UserSummaryApi
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = "";
        public string? MembershipTierName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Gender { get; set; }
        public string? SkillLevel { get; set; }
    }
}

public class ApiRoleService(ApiClient api) : IRoleService
{
    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await api.GetDataAsync<List<RoleDto>>("api/roles");
        return roles ?? [];
    }

    public async Task<List<PermissionMatrixRow>> GetPermissionMatrixAsync()
    {
        var rows = await api.GetDataAsync<List<PermissionMatrixRow>>("api/roles/permission-matrix");
        return rows ?? [];
    }

    public async Task UpdatePermissionMatrixAsync(List<PermissionMatrixRow> matrix)
    {
        await api.PutDataAsync<object>("api/roles/permission-matrix", matrix);
    }
}
