using System;

namespace SportCourtManagement_FrontEnd.Models.Courts;

public class CourtListDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string CourtCode { get; set; } = string.Empty;
    public string CourtTypeName { get; set; } = string.Empty;
    public int CourtTypeId { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public string? Surface { get; set; }
    public int? Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
