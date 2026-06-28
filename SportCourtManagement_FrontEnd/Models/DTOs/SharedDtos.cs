namespace SportCourtManagement_FrontEnd.Models.DTOs;

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Customer";
    public string? MembershipTier { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public class VerifyEmailRequest
{
    public string Email { get; set; } = "";
    public string Otp { get; set; } = "";
}

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public UserDto User { get; set; } = new();
}

<<<<<<< HEAD
public class AuthLoginResult
{
    public AuthResponse? Response { get; init; }
    public string? ErrorMessage { get; init; }
    public bool RequiresEmailVerification { get; init; }

    public bool Succeeded => Response != null;

    public static AuthLoginResult Ok(AuthResponse response) => new() { Response = response };

    public static AuthLoginResult Fail(string message, bool requiresVerification = false) =>
        new() { ErrorMessage = message, RequiresEmailVerification = requiresVerification };
}

=======
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
public class CourtTypeDto
{
    public int CourtTypeId { get; set; }
    public string TypeName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class CourtComplexDto
{
    public int ComplexId { get; set; }
    public string ComplexName { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Phone { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int TotalCourts { get; set; }
    public int ActiveCourts { get; set; }
    public int MaintenanceCourts { get; set; }
    public int InactiveCourts { get; set; }
    public List<int> CourtTypeIds { get; set; } = [];
    public DateTime? CreatedAt { get; set; }
}

public class CourtDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = "";
    public string CourtCode { get; set; } = "";
    public int CourtTypeId { get; set; }
    public string? CourtTypeName { get; set; }
    public int? ComplexId { get; set; }
    public string? ComplexName { get; set; }
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
    public int Capacity { get; set; } = 4;
    public string? Surface { get; set; }
    public string ImageUrl { get; set; } = "";
    public string Status { get; set; } = "Available";
    public string OpenTime { get; set; } = "06:00";
    public string CloseTime { get; set; } = "22:00";
    public decimal PricePerHour { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public string? CourtSize { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ComplexStatsDto
{
    public int TotalComplexes { get; set; }
    public int TotalCourts { get; set; }
    public int ActiveCourts { get; set; }
    public int MaintenanceCourts { get; set; }
    public int InactiveCourts { get; set; }
}

public class ServiceDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public string? Unit { get; set; }
    public string? Description { get; set; }
    public int StockQty { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";
    public string? Description { get; set; }
    public int UserCount { get; set; }
}

public class DashboardSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
    public double OccupancyRate { get; set; }
    public int ActiveCustomersCount { get; set; }
    public List<TopRatedCourtDto> TopRatedCourts { get; set; } = [];
}

public class TopRatedCourtDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = "";
    public decimal Rating { get; set; }
}

public class RevenueDataPointDto
{
    public string Label { get; set; } = "";
    public decimal Revenue { get; set; }
    public int Bookings { get; set; }
}

public class CourtUsageDto
{
    public string TimeSlot { get; set; } = "";
    public double OccupancyPercent { get; set; }
}

public class PermissionMatrixRow
{
    public string Feature { get; set; } = "";
    public bool Admin { get; set; }
    public bool Manager { get; set; }
    public bool Staff { get; set; }
    public bool Coach { get; set; }
    public bool Customer { get; set; }
}
