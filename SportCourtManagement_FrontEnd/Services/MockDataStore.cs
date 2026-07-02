using SportCourtManagement_FrontEnd.Models.DTOs;

namespace SportCourtManagement_FrontEnd.Services;

/// <summary>
/// In-memory data store for UI preview when API is not available.
/// </summary>
public class MockDataStore
{
    private int _nextComplexId = 10;
    private int _nextCourtId = 20;
    private int _nextServiceId = 20;
    private int _nextOfferingId = 100;
    private int _nextUserId = 20;

    public List<UserDto> Users { get; } =
    [
        new() { UserId = 1, FullName = "Admin System", Email = "admin@sportscourtms.vn", Phone = "0901000001", Role = "Admin", MembershipTier = "Platinum", IsActive = true, AvatarUrl = "https://api.dicebear.com/8.x/avataaars/svg?seed=admin", CreatedAt = new DateTime(2026, 1, 1) },
        new() { UserId = 2, FullName = "Nguyễn Văn Hùng", Email = "customer@gmail.com", Phone = "0901000002", Role = "Customer", MembershipTier = "Silver", IsActive = true, AvatarUrl = "https://api.dicebear.com/8.x/avataaars/svg?seed=hung", CreatedAt = new DateTime(2026, 2, 10) },
        new() { UserId = 3, FullName = "Trần Thị Mai", Email = "staff@sportscourtms.vn", Phone = "0901000003", Role = "Staff", MembershipTier = "Bronze", IsActive = true, AvatarUrl = "https://api.dicebear.com/8.x/avataaars/svg?seed=mai", CreatedAt = new DateTime(2026, 1, 15) },
        new() { UserId = 4, FullName = "Lê Minh Tuấn", Email = "coach@sportscourtms.vn", Phone = "0901000004", Role = "Coach", MembershipTier = "Bronze", IsActive = true, AvatarUrl = "https://api.dicebear.com/8.x/avataaars/svg?seed=tuan", CreatedAt = new DateTime(2026, 1, 20) },
        new() { UserId = 10, FullName = "Trần Văn Quản Lý", Email = "manager1@sportplex.vn", Phone = "0912111222", Role = "Staff", MembershipTier = "Bronze", IsActive = true, AvatarUrl = "https://api.dicebear.com/8.x/avataaars/svg?seed=quanly1", CreatedAt = new DateTime(2026, 1, 5) },
        new() { UserId = 11, FullName = "Nguyễn Thị Quản Lý", Email = "manager2@sportplex.vn", Phone = "0987333444", Role = "Staff", MembershipTier = "Bronze", IsActive = true, AvatarUrl = "https://api.dicebear.com/8.x/avataaars/svg?seed=quanly2", CreatedAt = new DateTime(2026, 1, 5) },
    ];

    public Dictionary<string, string> Passwords { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin@sportscourtms.vn"] = "Admin@123",
        ["customer@gmail.com"] = "Customer@123",
        ["staff@sportscourtms.vn"] = "Staff@123",
        ["coach@sportscourtms.vn"] = "Coach@123",
    };

    public Dictionary<string, string> PendingOtps { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<CourtTypeDto> CourtTypes { get; } =
    [
        new() { CourtTypeId = 1, TypeName = "Pickleball", IsActive = true },
        new() { CourtTypeId = 2, TypeName = "Cầu lông", IsActive = true },
        new() { CourtTypeId = 3, TypeName = "Bóng đá mini", IsActive = true },
        new() { CourtTypeId = 4, TypeName = "Tennis", IsActive = true },
        new() { CourtTypeId = 5, TypeName = "Bóng rổ", IsActive = true },
    ];

    public List<CourtComplexDto> Complexes { get; } =
    [
        new() { ComplexId = 1, ComplexName = "Tổ hợp thể thao Cầu Giấy", Address = "Dịch Vọng, Cầu Giấy, Hà Nội", Phone = "024 3823 1234", ManagerId = 10, ManagerName = "Trần Văn Quản Lý", Description = "Tổ hợp thể thao đa năng tại Cầu Giấy với hệ thống sân Pickleball và Cầu lông tiêu chuẩn quốc tế.", ImageUrl = "https://images.unsplash.com/photo-1574623452334-1e0ac2b3ccb4?w=800", CourtTypeIds = [1, 2], CreatedAt = new DateTime(2026, 1, 1) },
        new() { ComplexId = 2, ComplexName = "Tổ hợp thể thao Thanh Xuân", Address = "Nguyễn Trãi, Thanh Xuân, Hà Nội", Phone = "024 3521 8888", ManagerId = 11, ManagerName = "Nguyễn Thị Quản Lý", Description = "Khu thể thao tổng hợp với sân bóng đá mini, cầu lông và tennis ngay tại trung tâm Thanh Xuân.", ImageUrl = "https://images.unsplash.com/photo-1569517282132-25d22f4573e6?w=800", CourtTypeIds = [2, 3], CreatedAt = new DateTime(2026, 1, 15) },
    ];

    public List<CourtDto> Courts { get; } =
    [
        new() { CourtId = 1, CourtName = "Sân Pickleball A1", CourtCode = "PCK-A1", CourtTypeId = 1, CourtTypeName = "Pickleball", ComplexId = 1, ComplexName = "Tổ hợp thể thao Cầu Giấy", Description = "Sân pickleball tiêu chuẩn quốc tế.", Location = "Khu A - Tầng 1", Capacity = 4, Surface = "Polymer cao cấp", ImageUrl = "https://images.unsplash.com/photo-1554068865-24cecd4e34b8?w=800", Status = "Available", OpenTime = "06:00", CloseTime = "22:00", PricePerHour = 100000, Rating = 4.8m, ReviewCount = 124, CreatedAt = new DateTime(2026, 1, 1) },
        new() { CourtId = 2, CourtName = "Sân Pickleball A2", CourtCode = "PCK-A2", CourtTypeId = 1, CourtTypeName = "Pickleball", ComplexId = 1, ComplexName = "Tổ hợp thể thao Cầu Giấy", Description = "Sân pickleball tiêu chuẩn.", Location = "Khu A - Tầng 1", Capacity = 4, Surface = "Polymer cao cấp", ImageUrl = "https://images.unsplash.com/photo-1551698618-1dfe5d97d256?w=800", Status = "Available", OpenTime = "06:00", CloseTime = "22:00", PricePerHour = 100000, Rating = 4.6m, ReviewCount = 89, CreatedAt = new DateTime(2026, 1, 1) },
        new() { CourtId = 3, CourtName = "Sân Pickleball VIP", CourtCode = "PCK-VIP", CourtTypeId = 1, CourtTypeName = "Pickleball", ComplexId = 1, ComplexName = "Tổ hợp thể thao Cầu Giấy", Description = "Sân VIP với khán đài 50 chỗ.", Location = "Khu B - Tầng 2", Capacity = 4, Surface = "Nhựa PVC cao cấp", ImageUrl = "https://images.unsplash.com/photo-1544298338-0ea56c26f56f?w=800", Status = "Booked", OpenTime = "06:00", CloseTime = "22:00", PricePerHour = 150000, Rating = 4.9m, ReviewCount = 57, CreatedAt = new DateTime(2026, 1, 1) },
        new() { CourtId = 4, CourtName = "Sân Cầu Lông B1", CourtCode = "CL-B1", CourtTypeId = 2, CourtTypeName = "Cầu lông", ComplexId = 2, ComplexName = "Tổ hợp thể thao Thanh Xuân", Description = "Sân cầu lông tiêu chuẩn BWF.", Location = "Khu B - Tầng 1", Capacity = 4, Surface = "Gỗ tự nhiên", ImageUrl = "https://images.unsplash.com/photo-1626224583764-f87db24ac4ea?w=800", Status = "Available", OpenTime = "06:00", CloseTime = "22:00", PricePerHour = 80000, Rating = 4.5m, ReviewCount = 201, CreatedAt = new DateTime(2026, 1, 1) },
        new() { CourtId = 5, CourtName = "Sân Cầu Lông B2", CourtCode = "CL-B2", CourtTypeId = 2, CourtTypeName = "Cầu lông", ComplexId = 2, ComplexName = "Tổ hợp thể thao Thanh Xuân", Description = "Sân cầu lông ngoài trời có mái che.", Location = "Khu B - Ngoài trời", Capacity = 4, Surface = "Nhựa PVC", ImageUrl = "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800", Status = "Maintenance", OpenTime = "06:00", CloseTime = "20:00", PricePerHour = 70000, Rating = 4.3m, ReviewCount = 145, CreatedAt = new DateTime(2026, 1, 1) },
        new() { CourtId = 6, CourtName = "Sân Bóng Đá Mini C1", CourtCode = "BD-C1", CourtTypeId = 3, CourtTypeName = "Bóng đá mini", ComplexId = 2, ComplexName = "Tổ hợp thể thao Thanh Xuân", Description = "Sân bóng đá mini 5 người.", Location = "Khu C - Tầng trệt", Capacity = 10, Surface = "Cỏ nhân tạo", ImageUrl = "https://images.unsplash.com/photo-1529900748604-07564a03e7a6?w=800", Status = "Available", OpenTime = "06:00", CloseTime = "22:00", PricePerHour = 200000, Rating = 4.7m, ReviewCount = 312, CreatedAt = new DateTime(2026, 1, 1) },
    ];

    public List<ServiceDto> Services { get; } =
    [
        new() { ServiceId = 1, ServiceName = "Thuê vợt Pickleball", Category = "Equipment", Price = 30000, Unit = "Giờ", Description = "Vợt carbon cao cấp", StockQty = 25, IsActive = true },
        new() { ServiceId = 2, ServiceName = "Thuê bóng Pickleball", Category = "Equipment", Price = 10000, Unit = "Quả", Description = "Bóng tiêu chuẩn USAPA", StockQty = 50, IsActive = true },
        new() { ServiceId = 3, ServiceName = "Nước suối Lavie", Category = "Drink", Price = 10000, Unit = "Chai", Description = "500ml", StockQty = 200, IsActive = true },
        new() { ServiceId = 4, ServiceName = "Nước tăng lực Red Bull", Category = "Drink", Price = 25000, Unit = "Lon", Description = "250ml", StockQty = 80, IsActive = true },
        new() { ServiceId = 5, ServiceName = "HLV Pickleball cơ bản", Category = "Coach", Price = 300000, Unit = "Giờ", Description = "HLV có chứng chỉ quốc tế", StockQty = 5, IsActive = true },
        new() { ServiceId = 6, ServiceName = "HLV Cầu lông nâng cao", Category = "Coach", Price = 400000, Unit = "Giờ", Description = "Kèm 1-1", StockQty = 3, IsActive = true },
        new() { ServiceId = 7, ServiceName = "Tổ chức giải đấu mini", Category = "Event", Price = 5000000, Unit = "Sự kiện", Description = "Bao gồm trọng tài và giải thưởng", StockQty = 2, IsActive = true },
        new() { ServiceId = 8, ServiceName = "Thuê lưới dự phòng", Category = "Equipment", Price = 50000, Unit = "Bộ", Description = "Lưới tiêu chuẩn thi đấu", StockQty = 10, IsActive = true },
    ];

    public List<ComplexCourtTypeServiceDto> ServiceOfferings { get; } =
    [
        new() { OfferingId = 1, ComplexId = 1, CourtTypeId = 1, CourtTypeName = "Pickleball", ServiceId = 3, ServiceName = "Nước suối Lavie", Category = "Drink", Unit = "Chai", Price = 0, StockQty = 100, ServiceMode = "Included", IsActive = true },
        new() { OfferingId = 2, ComplexId = 1, CourtTypeId = 1, CourtTypeName = "Pickleball", ServiceId = 1, ServiceName = "Thuê vợt Pickleball", Category = "Equipment", Unit = "Giờ", Price = 30000, StockQty = 25, ServiceMode = "Optional", IsActive = true },
        new() { OfferingId = 3, ComplexId = 1, CourtTypeId = 2, CourtTypeName = "Cầu lông", ServiceId = 3, ServiceName = "Nước suối Lavie", Category = "Drink", Unit = "Chai", Price = 0, StockQty = 80, ServiceMode = "Included", IsActive = true },
    ];

    public List<RoleDto> Roles { get; } =
    [
        new() { RoleId = 1, RoleName = "Admin", Description = "Quản trị viên hệ thống — toàn quyền", UserCount = 1 },
        new() { RoleId = 2, RoleName = "Staff", Description = "Nhân viên hỗ trợ đặt sân và khách hàng", UserCount = 2 },
        new() { RoleId = 3, RoleName = "Coach", Description = "Huấn luyện viên — quản lý lịch dạy", UserCount = 1 },
        new() { RoleId = 4, RoleName = "Customer", Description = "Khách hàng đặt sân", UserCount = 1 },
    ];

    public List<PermissionMatrixRow> PermissionMatrix { get; } =
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
        new() { Feature = "Phân quyền hệ thống", Admin = true, Manager = false, Staff = false, Coach = false, Customer = false },
    ];

    public MockDataStore()
    {
        RecalculateComplexStats();
    }

    public void RecalculateComplexStats()
    {
        foreach (var complex in Complexes)
        {
            var courts = Courts.Where(c => c.ComplexId == complex.ComplexId).ToList();
            complex.TotalCourts = courts.Count;
            complex.ActiveCourts = courts.Count(c => c.Status is "Available" or "Booked" or "InUse" or "Active");
            complex.MaintenanceCourts = courts.Count(c => c.Status == "Maintenance");
            complex.InactiveCourts = courts.Count(c => c.Status == "Inactive");
        }
    }

    public ComplexStatsDto GetComplexStats() => new()
    {
        TotalComplexes = Complexes.Count,
        TotalCourts = Courts.Count,
        ActiveCourts = Courts.Count(c => c.Status is "Available" or "Booked" or "InUse" or "Active"),
        MaintenanceCourts = Courts.Count(c => c.Status == "Maintenance"),
        InactiveCourts = Courts.Count(c => c.Status == "Inactive"),
    };

    public int NextComplexId() => _nextComplexId++;
    public int NextCourtId() => _nextCourtId++;
    public int NextServiceId() => _nextServiceId++;
    public int NextOfferingId() => _nextOfferingId++;
    public int NextUserId() => _nextUserId++;
}
