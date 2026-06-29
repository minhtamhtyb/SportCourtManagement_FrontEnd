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

    public async Task UpdateUserRoleAsync(int id, string role)
    {
        await api.PutDataAsync<UserSummaryApi>($"api/users/{id}/role", new { role });
    }

    public async Task ToggleUserStatusAsync(int id, bool isActive)
    {
        await api.PatchDataAsync<UserSummaryApi>($"api/users/{id}/status", new { isActive });
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
        CreatedAt = u.CreatedAt
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
}
