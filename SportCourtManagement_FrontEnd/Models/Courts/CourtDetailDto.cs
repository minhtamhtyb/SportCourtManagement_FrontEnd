using System;
using System.Collections.Generic;
using SportCourtManagement_FrontEnd.Models.Reviews;

namespace SportCourtManagement_FrontEnd.Models.Courts;

public class CourtDetailDto
{
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public string CourtCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Surface { get; set; }
    public int? Capacity { get; set; }
    public string? ImageUrl { get; set; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public CourtTypeDto CourtType { get; set; } = null!;
    public List<CourtImageDto> Images { get; set; } = new();
    public List<CourtPricingDto> Pricings { get; set; } = new();
    public CourtReviewSummaryDto ReviewSummary { get; set; } = null!;
}

public class CourtImageDto
{
    public int ImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}

public class CourtPricingDto
{
    public int PricingId { get; set; }
    public int SlotId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string DayType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal PeakMultiplier { get; set; }
}
