using System;
using System.Collections.Generic;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Services;

namespace SportCourtManagement_FrontEnd.Models.ViewModels;

/// <summary>
/// ViewModel for the singular Booking/Index form page.
/// Holds all selectable options for building a booking request.
/// </summary>
public class SingularBookingViewModel
{
    /// <summary>All available courts for selection.</summary>
    public List<CourtListDto> Courts { get; set; } = new();

    /// <summary>All available time slots.</summary>
    public List<TimeSlotDto> TimeSlots { get; set; } = new();

    /// <summary>All available add-on services.</summary>
    public List<ServiceDto> Services { get; set; } = new();

    /// <summary>Pre-selected court ID (if navigated from a court detail page).</summary>
    public int? SelectedCourtId { get; set; }

    /// <summary>Pre-selected date.</summary>
    public DateTime? SelectedDate { get; set; }

    /// <summary>Pre-selected slot ID.</summary>
    public int? SelectedSlotId { get; set; }
}
