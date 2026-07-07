using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SportCourtManagement_FrontEnd.Models.Bookings;

public class BookingRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Sân không hợp lệ")]
    public int CourtId { get; set; }

    public DateOnly BookingDate { get; set; }
    public List<int> TimeSlotIds { get; set; } = new();
    public string? PromotionCode { get; set; }
    public string? Note { get; set; }
    public List<BookingServiceRequestDto> Services { get; set; } = new();
}

public class BookingServiceRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Dịch vụ không hợp lệ")]
    public int ServiceId { get; set; }

    [Range(1, 1000, ErrorMessage = "Số lượng dịch vụ phải từ 1 đến 1000")]
    public int Quantity { get; set; }
}
