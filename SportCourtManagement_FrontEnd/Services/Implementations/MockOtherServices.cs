using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class MockServiceCatalogService(MockDataStore store) : IServiceCatalogService
{
    public Task<List<ServiceDto>> GetServicesAsync(string? category, string? search)
    {
        var query = store.Services.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(s => s.ServiceName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        return Task.FromResult(query.OrderBy(s => s.ServiceName).ToList());
    }

    public Task<ServiceDto?> GetServiceByIdAsync(int id) =>
        Task.FromResult(store.Services.FirstOrDefault(s => s.ServiceId == id));

    public Task<ServiceDto> CreateServiceAsync(ServiceDto dto)
    {
        dto.ServiceId = store.NextServiceId();
        store.Services.Add(dto);
        return Task.FromResult(dto);
    }

    public Task UpdateServiceAsync(int id, ServiceDto dto)
    {
        var existing = store.Services.FirstOrDefault(s => s.ServiceId == id)
            ?? throw new InvalidOperationException("Không tìm thấy dịch vụ.");
        existing.ServiceName = dto.ServiceName;
        existing.Category = dto.Category;
        existing.Price = dto.Price;
        existing.Unit = dto.Unit;
        existing.Description = dto.Description;
        existing.StockQty = dto.StockQty;
        existing.IsActive = dto.IsActive;
        return Task.CompletedTask;
    }

    public Task DeleteServiceAsync(int id)
    {
        var svc = store.Services.FirstOrDefault(s => s.ServiceId == id)
            ?? throw new InvalidOperationException("Không tìm thấy dịch vụ.");
        svc.IsActive = false;
        return Task.CompletedTask;
    }
}

public class MockReportService(MockDataStore store) : IReportService
{
    public Task<DashboardSummaryDto> GetDashboardAsync() =>
        Task.FromResult(new DashboardSummaryDto
        {
            TotalRevenue = 154_800_000,
            TotalBookings = 482,
            OccupancyRate = 68.5,
            ActiveCustomersCount = 182,
            TopRatedCourts =
            [
                new() { CourtId = 3, CourtName = "Sân Pickleball VIP", Rating = 4.9m },
                new() { CourtId = 1, CourtName = "Sân Pickleball A1", Rating = 4.8m },
                new() { CourtId = 6, CourtName = "Sân Bóng Đá Mini C1", Rating = 4.7m },
            ]
        });

    public Task<List<RevenueDataPointDto>> GetRevenueReportAsync(string period)
    {
        List<RevenueDataPointDto> data = period switch
        {
            "Week" =>
            [
                new() { Label = "T2", Revenue = 12_500_000, Bookings = 35 },
                new() { Label = "T3", Revenue = 15_200_000, Bookings = 42 },
                new() { Label = "T4", Revenue = 18_800_000, Bookings = 51 },
                new() { Label = "T5", Revenue = 14_300_000, Bookings = 38 },
                new() { Label = "T6", Revenue = 22_100_000, Bookings = 58 },
                new() { Label = "T7", Revenue = 28_500_000, Bookings = 72 },
                new() { Label = "CN", Revenue = 25_400_000, Bookings = 65 },
            ],
            "Year" =>
            [
                new() { Label = "T1", Revenue = 120_000_000, Bookings = 320 },
                new() { Label = "T2", Revenue = 98_000_000, Bookings = 280 },
                new() { Label = "T3", Revenue = 135_000_000, Bookings = 350 },
                new() { Label = "T4", Revenue = 154_800_000, Bookings = 482 },
            ],
            _ =>
            [
                new() { Label = "Tuần 1", Revenue = 32_000_000, Bookings = 95 },
                new() { Label = "Tuần 2", Revenue = 38_500_000, Bookings = 110 },
                new() { Label = "Tuần 3", Revenue = 41_200_000, Bookings = 125 },
                new() { Label = "Tuần 4", Revenue = 43_100_000, Bookings = 152 },
            ]
        };
        return Task.FromResult(data);
    }

    public Task<List<CourtUsageDto>> GetCourtUsageAsync() =>
        Task.FromResult(new List<CourtUsageDto>
        {
            new() { TimeSlot = "06:00-08:00", OccupancyPercent = 35 },
            new() { TimeSlot = "08:00-10:00", OccupancyPercent = 55 },
            new() { TimeSlot = "10:00-12:00", OccupancyPercent = 72 },
            new() { TimeSlot = "12:00-14:00", OccupancyPercent = 45 },
            new() { TimeSlot = "14:00-16:00", OccupancyPercent = 60 },
            new() { TimeSlot = "16:00-18:00", OccupancyPercent = 85 },
            new() { TimeSlot = "18:00-20:00", OccupancyPercent = 95 },
            new() { TimeSlot = "20:00-22:00", OccupancyPercent = 78 },
        });

    public Task<ComplexStatsDto> GetComplexStatsAsync() =>
        Task.FromResult(store.GetComplexStats());
}

public class MockUserService(MockDataStore store) : IUserService
{
    public Task<Models.Api.PagedResult<UserDto>> GetUsersAsync(string? search, string? role, int page, int pageSize)
    {
        var query = store.Users.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(u =>
                u.FullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (u.Phone?.Contains(q) ?? false));
        }
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var list = query.OrderBy(u => u.FullName).ToList();
        var total = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Task.FromResult(new Models.Api.PagedResult<UserDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        });
    }

    public Task<UserDto?> GetUserByIdAsync(int id) =>
        Task.FromResult(store.Users.FirstOrDefault(u => u.UserId == id));

    public Task UpdateUserRoleAsync(int id, string role)
    {
        var user = store.Users.FirstOrDefault(u => u.UserId == id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        user.Role = role;
        foreach (var r in store.Roles)
            r.UserCount = store.Users.Count(u => u.Role == r.RoleName);
        return Task.CompletedTask;
    }

    public Task ToggleUserStatusAsync(int id, bool isActive)
    {
        var user = store.Users.FirstOrDefault(u => u.UserId == id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        user.IsActive = isActive;
        return Task.CompletedTask;
    }
}

public class MockRoleService(MockDataStore store) : IRoleService
{
    public Task<List<RoleDto>> GetRolesAsync()
    {
        foreach (var r in store.Roles)
            r.UserCount = store.Users.Count(u => u.Role == r.RoleName);
        return Task.FromResult(store.Roles.ToList());
    }

    public Task<List<PermissionMatrixRow>> GetPermissionMatrixAsync() =>
        Task.FromResult(store.PermissionMatrix.ToList());
}
