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
    public Task<AdminDashboardDto> GetAdminDashboardAsync() =>
        Task.FromResult(new AdminDashboardDto
        {
            Kpis = new AdminDashboardKpiDto
            {
                TodayRevenue = 4_500_000,
                MonthRevenue = 85_000_000,
                TodayBookings = 12,
                PendingBookings = 2,
                OccupancyRate = 75.0,
                ActiveCustomers = 45,
                ActiveCoaches = 6,
                TotalCourts = 8,
                AvailableCourts = 5,
                MaintenanceCourts = 1,
                InactiveCourts = 0,
                LowStockServices = 3
            },
            RevenueChart =
            [
                new() { Date = "13/07", Revenue = 3200000, Bookings = 8 },
                new() { Date = "14/07", Revenue = 4100000, Bookings = 10 },
                new() { Date = "15/07", Revenue = 3800000, Bookings = 9 },
                new() { Date = "16/07", Revenue = 5200000, Bookings = 14 },
                new() { Date = "17/07", Revenue = 6100000, Bookings = 16 },
                new() { Date = "18/07", Revenue = 7400000, Bookings = 19 },
                new() { Date = "19/07", Revenue = 4500000, Bookings = 12 }
            ],
            RecentBookings =
            [
                new() { BookingId = 101, BookingCode = "BK20260719-01", CustomerName = "Nguyễn Văn A", CourtName = "Sân Cầu Lông A1", CourtTypeName = "Sân Cầu Lông", BookingDate = "19/07/2026", SlotName = "Ca Sáng 1 (07:00 - 09:00)", TotalAmount = 240000, Status = "Confirmed" },
                new() { BookingId = 102, BookingCode = "BK20260719-02", CustomerName = "Trần Thị B", CourtName = "Sân Tennis T1", CourtTypeName = "Sân Tennis", BookingDate = "19/07/2026", SlotName = "Ca Chiều (14:00 - 17:00)", TotalAmount = 500000, Status = "Pending" },
                new() { BookingId = 103, BookingCode = "BK20260719-03", CustomerName = "Lê Hoàng C", CourtName = "Sân Pickleball P1", CourtTypeName = "Sân Pickleball", BookingDate = "19/07/2026", SlotName = "Ca Tối Vàng (17:30 - 21:00)", TotalAmount = 450000, Status = "Confirmed" }
            ],
            CourtGrid =
            [
                new() { CourtId = 1, CourtName = "Sân Cầu Lông A1 (VIP)", CourtCode = "CL-A1", CourtType = "Sân Cầu Lông", Status = "Available", PricePerHour = 120000 },
                new() { CourtId = 2, CourtName = "Sân Cầu Lông A2", CourtCode = "CL-A2", CourtType = "Sân Cầu Lông", Status = "Available", PricePerHour = 100000 },
                new() { CourtId = 3, CourtName = "Sân Tennis Trung Tâm T1", CourtCode = "TN-T1", CourtType = "Sân Tennis", Status = "Maintenance", PricePerHour = 250000 },
                new() { CourtId = 4, CourtName = "Sân Pickleball P1", CourtCode = "PK-P1", CourtType = "Sân Pickleball", Status = "Available", PricePerHour = 150000 },
                new() { CourtId = 5, CourtName = "Sân Bóng Đá Mini S1", CourtCode = "BD-S1", CourtType = "Sân Bóng Đá", Status = "Available", PricePerHour = 400000 }
            ],
            Alerts =
            [
                new() { Type = "warning", Icon = "fa-clock", Message = "2 booking đang chờ xác nhận" },
                new() { Type = "warning", Icon = "fa-box-open", Message = "3 dịch vụ tồn kho dưới 10 đơn vị" },
                new() { Type = "info", Icon = "fa-wrench", Message = "1 sân đang trong chế độ bảo trì" }
            ],
            TopCustomers =
            [
                new() { UserId = 1, FullName = "Nguyễn Văn A", TotalSpend = 1450000, BookingCount = 5 },
                new() { UserId = 2, FullName = "Trần Thị B", TotalSpend = 1100000, BookingCount = 3 },
                new() { UserId = 3, FullName = "Lê Hoàng C", TotalSpend = 950000, BookingCount = 4 }
            ]
        });

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

    public Task UpdateUserAccessAsync(int id, string role, bool isActive)
    {
        var user = store.Users.FirstOrDefault(u => u.UserId == id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

        ValidateAccessChange(user, role, isActive);
        user.Role = role;
        user.IsActive = isActive;
        RecountRoles();
        return Task.CompletedTask;
    }

    public Task UpdateUserRoleAsync(int id, string role)
    {
        var user = store.Users.FirstOrDefault(u => u.UserId == id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        return UpdateUserAccessAsync(id, role, user.IsActive);
    }

    public Task ToggleUserStatusAsync(int id, bool isActive)
    {
        var user = store.Users.FirstOrDefault(u => u.UserId == id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");
        return UpdateUserAccessAsync(id, user.Role, isActive);
    }

    public Task<UserDto> CreateUserAsync(UserDto dto, string password)
    {
        if (store.Users.Any(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Email này đã được đăng ký.");
        if (!string.IsNullOrWhiteSpace(dto.Phone) && store.Users.Any(u => u.Phone == dto.Phone))
            throw new InvalidOperationException("Số điện thoại này đã được đăng ký.");

        dto.UserId = store.Users.Max(u => u.UserId) + 1;
        dto.CreatedAt = DateTime.Now;
        dto.MembershipTier = "Bronze";
        store.Users.Add(dto);
        RecountRoles();
        return Task.FromResult(dto);
    }

    public Task UpdateUserByAdminAsync(int id, UserDto dto)
    {
        var user = store.Users.FirstOrDefault(u => u.UserId == id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

        if (!user.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase) &&
            store.Users.Any(u => u.Email.Equals(dto.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Email này đã được đăng ký bởi người dùng khác.");

        if (!string.Equals(user.Phone, dto.Phone) && !string.IsNullOrWhiteSpace(dto.Phone) &&
            store.Users.Any(u => u.Phone == dto.Phone))
            throw new InvalidOperationException("Số điện thoại này đã được đăng ký bởi người dùng khác.");

        ValidateAccessChange(user, dto.Role, dto.IsActive);

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.Phone = dto.Phone;
        user.Role = dto.Role;
        user.Gender = dto.Gender;
        user.SkillLevel = dto.SkillLevel;
        user.IsActive = dto.IsActive;
        RecountRoles();
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(int id)
    {
        var user = store.Users.FirstOrDefault(u => u.UserId == id)
            ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

        if (user.Role == "Admin")
        {
            var activeAdminCount = store.Users.Count(u => u.Role == "Admin" && u.IsActive);
            if (activeAdminCount <= 1)
                throw new InvalidOperationException("Không thể xóa Admin cuối cùng của hệ thống.");
        }

        store.Users.Remove(user);
        RecountRoles();
        return Task.CompletedTask;
    }

    private void ValidateAccessChange(UserDto user, string role, bool isActive)
    {
        if (user.UserId == 1)
        {
            if (role != "Admin")
                throw new InvalidOperationException("Bạn không thể tự hạ quyền Admin của chính mình.");
            if (!isActive)
                throw new InvalidOperationException("Bạn không thể tự vô hiệu hóa tài khoản của chính mình.");
        }

        if (user.Role == "Admin" && (role != "Admin" || !isActive))
        {
            var activeAdminCount = store.Users.Count(u => u.Role == "Admin" && u.IsActive);
            var stillActiveAdmin = role == "Admin" && isActive;
            if (!stillActiveAdmin && activeAdminCount <= 1)
                throw new InvalidOperationException("Không thể thay đổi — hệ thống cần ít nhất một Admin đang hoạt động.");
        }
    }

    private void RecountRoles()
    {
        foreach (var r in store.Roles)
            r.UserCount = store.Users.Count(u => u.Role == r.RoleName);
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
