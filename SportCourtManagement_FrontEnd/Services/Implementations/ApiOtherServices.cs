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
        var query = "api/users";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
            parts.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(role))
            parts.Add($"role={Uri.EscapeDataString(role)}");
        if (parts.Count > 0)
            query += "?" + string.Join("&", parts);

        var users = (await api.GetDataAsync<List<UserSummaryApi>>(query) ?? [])
            .Select(MapUser).ToList();

        var total = users.Count;
        var items = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PagedResult<UserDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
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
        IsActive = u.IsActive
    };

    private class UserSummaryApi
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
    }
}

public class ApiRoleService(ApiClient api) : IRoleService
{
    public async Task<List<RoleDto>> GetRolesAsync()
    {
        var roles = await api.GetDataAsync<List<RoleDto>>("api/roles");
        return roles ?? [];
    }

    public Task<List<PermissionMatrixRow>> GetPermissionMatrixAsync() =>
        Task.FromResult(StaticPermissionMatrix.Rows);
}

internal static class StaticPermissionMatrix
{
    public static List<PermissionMatrixRow> Rows { get; } =
    [
        new() { Feature = "Quản lý sân", Admin = true, Manager = true, Staff = false, Coach = false, Customer = false },
        new() { Feature = "Quản lý đặt sân", Admin = true, Manager = true, Staff = true, Coach = false, Customer = true },
        new() { Feature = "Quản lý khách hàng", Admin = true, Manager = true, Staff = true, Coach = false, Customer = false },
        new() { Feature = "Thống kê doanh thu", Admin = true, Manager = true, Staff = false, Coach = false, Customer = false },
        new() { Feature = "Quản lý dịch vụ", Admin = true, Manager = true, Staff = true, Coach = false, Customer = false },
        new() { Feature = "Quản lý lịch dạy", Admin = true, Manager = true, Staff = false, Coach = true, Customer = false },
        new() { Feature = "Đặt sân", Admin = true, Manager = true, Staff = true, Coach = true, Customer = true },
        new() { Feature = "Đánh giá", Admin = false, Manager = false, Staff = false, Coach = false, Customer = true },
        new() { Feature = "Quản lý khuyến mãi", Admin = true, Manager = false, Staff = false, Coach = false, Customer = false },
        new() { Feature = "Quản lý nhân viên", Admin = true, Manager = true, Staff = false, Coach = false, Customer = false },
    ];
}
