using System.Collections.Generic;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Reviews;

namespace SportCourtManagement_FrontEnd.Models.ViewModels;

public class CourtDetailViewModel
{
    public CourtDetailDto Court { get; set; } = null!;
    public CourtAvailabilityDto Availability { get; set; } = null!;
    public PagedResult<ReviewDto> ReviewsResult { get; set; } = new();
    public List<CourtListDto> SimilarCourts { get; set; } = new();
    public DateOnly SelectedDate { get; set; }
}
