using System;

namespace SportCourtManagement_FrontEnd.Models.Courts;

public class CourtSearchParams
{
    public int? CourtTypeId { get; set; }
    public string? Status { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateOnly? Date { get; set; }
    public int? TimeSlotId { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
