using System;
using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Bookings;

public class BookingResponseDto
{
    public int BookingId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public DateOnly BookingDate { get; set; }
    public List<BookingSlotResponseDto> Slots { get; set; } = new();
    public decimal SubTotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServicesAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BookingSlotResponseDto
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}
