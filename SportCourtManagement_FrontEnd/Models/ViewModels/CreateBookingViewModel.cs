using System;
using SportCourtManagement_FrontEnd.Models.Courts;

namespace SportCourtManagement_FrontEnd.Models.ViewModels;

public class CreateBookingViewModel
{
    public CourtDetailDto Court { get; set; } = null!;
    public DateOnly BookingDate { get; set; }
    public AvailabilitySlotDto SelectedSlot { get; set; } = null!;
    public string? Token { get; set; }
}
