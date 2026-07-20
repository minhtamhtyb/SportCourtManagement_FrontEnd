using System;
using System.Collections.Generic;

namespace SportCourtManagement_FrontEnd.Models.Bookings;

/// <summary>
/// Response DTO matching the backend singular BookingController's BookingResponseDto.
/// Different from the existing BookingResponseDto which uses Slots list.
/// </summary>
public class SingularBookingResponseDto
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int CourtId { get; set; }
    public string CourtName { get; set; } = string.Empty;
    public int SlotId { get; set; }
    public string SlotName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ServicesAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PromoCode { get; set; }
    public string? Note { get; set; }
    public List<SingularBookingServiceResponseDto> BookingServices { get; set; } = new();
}

public class SingularBookingServiceResponseDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
}
