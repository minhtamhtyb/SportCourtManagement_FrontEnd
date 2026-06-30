using System;

namespace SportCourtManagement_FrontEnd.Models.Reviews;

public class ReviewDto
{
    public int ReviewId { get; set; }
    public int BookingId { get; set; }
    public int CourtId { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string? ImageUrl { get; set; }
    public string? AdminReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourtReviewSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
}
