using SportCourtManagement_FrontEnd.Models.DTOs;

namespace SportCourtManagement_FrontEnd.Services.Api;

internal class PagedComplexResult
{
    public List<CourtComplexDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public ComplexStatsDto Stats { get; set; } = new();
}

internal class ApiCourtDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = "";
    public string CourtCode { get; set; } = "";
    public int CourtTypeId { get; set; }
    public string? CourtTypeName { get; set; }
    public int? ComplexId { get; set; }
    public string? ComplexName { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public int? Capacity { get; set; }
    public string? Surface { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "";
    public string OpenTime { get; set; } = "";
    public string CloseTime { get; set; } = "";
    public decimal PricePerHour { get; set; }
    public string? CourtSize { get; set; }
    public List<string> ImageUrls { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
