using System;
using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Bookings;

public class BookingRequestDto
{
    public int CourtId { get; set; }
    public DateOnly BookingDate { get; set; }
    public List<int> TimeSlotIds { get; set; } = new();
    public string? PromotionCode { get; set; }
    public string? Note { get; set; }
    public List<BookingServiceRequestDto> Services { get; set; } = new();
}

public class BookingServiceRequestDto
{
    public int ServiceId { get; set; }
    public int Quantity { get; set; }
}
