using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SportCourtManagement_FrontEnd.Models.DTOs;

namespace SportCourtManagement_FrontEnd.Models.ViewModels.Admin;

public class ComplexListViewModel
{
    public List<CourtComplexDto> Complexes { get; set; } = [];
    public ComplexStatsDto Stats { get; set; } = new();
    public List<CourtTypeDto> CourtTypes { get; set; } = [];
    public string? Search { get; set; }
    public int? CourtTypeId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 8;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ComplexFormViewModel
{
    public int? ComplexId { get; set; }

    [Required(ErrorMessage = "Tên tổ hợp không được để trống")]
    [Display(Name = "Tên tổ hợp")]
    public string ComplexName { get; set; } = "";

    [Required(ErrorMessage = "Địa chỉ không được để trống")]
    [Display(Name = "Địa chỉ")]
    public string Address { get; set; } = "";

    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [Display(Name = "Mã quản lý")]
    [Required(ErrorMessage = "Vui lòng chọn quản lý phụ trách")]
    public int? ManagerId { get; set; }

    public string? ManagerName { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "URL ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Ảnh tổ hợp")]
    public IFormFile? ImageFile { get; set; }

    public List<int> CourtTypeIds { get; set; } = [];
    public List<CourtTypeDto> CourtTypeOptions { get; set; } = [];
    public List<UserDto> Managers { get; set; } = [];
}

public class ComplexDetailsViewModel
{
    public CourtComplexDto Complex { get; set; } = new();
    public UserDto? Manager { get; set; }
    public List<CourtDto> Courts { get; set; } = [];
    public List<CourtTypeDto> CourtTypes { get; set; } = [];
    public List<CourtTypeDto> ComplexCourtTypes { get; set; } = [];
    public string? Search { get; set; }
    public string? StatusFilter { get; set; }
}

public class ComplexServicesViewModel
{
    public CourtComplexDto Complex { get; set; } = new();
    public List<CourtTypeDto> ComplexCourtTypes { get; set; } = [];
    public List<ComplexCourtTypeServiceDto> ServiceOfferings { get; set; } = [];
    public int? SelectedCourtTypeId { get; set; }
}

public class ServiceOfferingFormViewModel
{
    public int ComplexId { get; set; }
    public int? OfferingId { get; set; }

    [Required]
    [Display(Name = "Loại sân")]
    public int CourtTypeId { get; set; }

    [Required]
    [Display(Name = "Dịch vụ")]
    public int ServiceId { get; set; }

    [Display(Name = "Giá (0 nếu Included)")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Display(Name = "Tồn kho")]
    [Range(0, int.MaxValue)]
    public int StockQty { get; set; }

    [Required]
    [Display(Name = "Hình thức")]
    public string ServiceMode { get; set; } = "Optional";

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public List<CourtTypeDto> CourtTypeOptions { get; set; } = [];
    public List<ServiceDto> CatalogServices { get; set; } = [];
    public List<string> ServiceModeOptions { get; set; } = ["Included", "Optional"];
}

public class CourtFormViewModel
{
    public int? CourtId { get; set; }
    public int ComplexId { get; set; }
    public string? ComplexName { get; set; }

    [Required]
    [Display(Name = "Tên sân")]
    public string CourtName { get; set; } = "";

    [Required]
    [Display(Name = "Mã sân")]
    public string CourtCode { get; set; } = "";

    [Required]
    [Display(Name = "Loại sân")]
    public int CourtTypeId { get; set; }

    [Display(Name = "Mô tả")]
    public string Description { get; set; } = "";

    [Display(Name = "Vị trí")]
    public string Location { get; set; } = "";

    [Display(Name = "Sức chứa")]
    public int Capacity { get; set; } = 4;

    [Display(Name = "Mặt sân")]
    public string? Surface { get; set; }

    [Display(Name = "URL ảnh")]
    public string ImageUrl { get; set; } = "";

    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Available";

    [Display(Name = "Giờ mở cửa")]
    public string OpenTime { get; set; } = "06:00";

    [Display(Name = "Giờ đóng cửa")]
    public string CloseTime { get; set; } = "22:00";

    [Display(Name = "Giá/giờ")]
    [Range(0, double.MaxValue)]
    public decimal PricePerHour { get; set; } = 100000;

    [Display(Name = "Kích thước")]
    public string? CourtSize { get; set; }

    public List<CourtTypeDto> CourtTypes { get; set; } = [];
    public List<string> StatusOptions { get; set; } = ["Available", "Maintenance", "Inactive", "Booked", "InUse"];
}

public class ServiceListViewModel
{
    public List<ServiceDto> Services { get; set; } = [];
    public string? Category { get; set; }
    public string? Search { get; set; }
}

public class ServiceFormViewModel
{
    public int? ServiceId { get; set; }

    [Required]
    [Display(Name = "Tên dịch vụ")]
    public string ServiceName { get; set; } = "";

    [Required]
    [Display(Name = "Loại")]
    public string Category { get; set; } = "Equipment";

    [Required]
    [Range(0, double.MaxValue)]
    [Display(Name = "Giá")]
    public decimal Price { get; set; }

    [Display(Name = "Đơn vị")]
    public string? Unit { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Tồn kho")]
    public int StockQty { get; set; }

    [Display(Name = "Đang hoạt động")]
    public bool IsActive { get; set; } = true;

    public List<string> CategoryOptions { get; set; } = ["Equipment", "Drink", "Coach", "Event"];
}

public class DashboardViewModel
{
    public DashboardSummaryDto Summary { get; set; } = new();
    public ComplexStatsDto CourtStats { get; set; } = new();
}

public class RevenueReportViewModel
{
    public string Period { get; set; } = "Month";
    public List<RevenueDataPointDto> DataPoints { get; set; } = [];
    public decimal TotalRevenue { get; set; }
    public int TotalBookings { get; set; }
}

public class CourtUsageReportViewModel
{
    public List<CourtUsageDto> UsageData { get; set; } = [];
}

public class UserListViewModel
{
    public List<UserDto> Users { get; set; } = [];
    public string? Search { get; set; }
    public string? Role { get; set; }
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<string> RoleOptions { get; set; } = ["Admin", "Staff", "Coach", "Customer"];
}

public class UserEditRolesViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";

    [Required]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Customer";

    public List<string> RoleOptions { get; set; } = ["Admin", "Staff", "Coach", "Customer"];
    public bool IsActive { get; set; } = true;
    public bool IsSelf { get; set; }
}

public class RoleListViewModel
{
    public List<RoleDto> Roles { get; set; } = [];
    public List<PermissionMatrixRow> PermissionMatrix { get; set; } = [];
}
