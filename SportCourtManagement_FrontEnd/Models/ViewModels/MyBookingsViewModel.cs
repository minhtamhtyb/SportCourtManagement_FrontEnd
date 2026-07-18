using System;
using System.Collections.Generic;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Bookings;

namespace SportCourtManagement_FrontEnd.Models.ViewModels;

/// <summary>
/// ViewModel for Customer's "My Bookings" page.
/// </summary>
public class MyBookingsPageViewModel
{
    public PagedResult<BookingDetailDto> PagedData { get; set; } = new();
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; }
}

/// <summary>
/// Request to change a booking to a different court.
/// </summary>
public class ChangeCourtRequestDto
{
    public int BookingId { get; set; }
    public int NewCourtId { get; set; }
    public int SlotId { get; set; }
    public string BookingDate { get; set; } = string.Empty;
}

/// <summary>
/// Request to add services to an existing booking.
/// </summary>
public class AddServiceRequestDto
{
    public int BookingId { get; set; }
    public List<ServiceItemRequestDto> Services { get; set; } = new();
}

public class ServiceItemRequestDto
{
    public int ServiceId { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Request to submit a review for a completed booking.
/// </summary>
public class SubmitReviewRequestDto
{
    public int BookingId { get; set; }
    public int CourtId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
}
