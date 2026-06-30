using System.Collections.Generic;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Courts;

namespace SportCourtManagement_FrontEnd.Models.ViewModels;

public class CourtSearchViewModel
{
    public PagedResult<CourtListDto> CourtsResult { get; set; } = new();
    public List<CourtTypeDto> CourtTypes { get; set; } = new();
    public CourtSearchParams SearchParams { get; set; } = new();
}
