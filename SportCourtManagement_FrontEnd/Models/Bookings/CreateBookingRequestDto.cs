using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SportCourtManagement_FrontEnd.Models.Bookings;

/// <summary>
/// DTO matching the backend singular BookingController's CreateBookingRequestDto.
/// </summary>
public class CreateBookingRequestDto
{
    [Required(ErrorMessage = "Vui lòng chọn sân.")]
    [Range(1, int.MaxValue, ErrorMessage = "Sân không hợp lệ.")]
    public int CourtId { get; set; }

    public int SlotId { get; set; }

    public List<int>? SlotIds { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày đặt sân.")]
    public DateTime BookingDate { get; set; }

    [StringLength(50, ErrorMessage = "Mã giảm giá không được vượt quá 50 ký tự.")]
    public string? PromoCode { get; set; }

    public List<CreateBookingServiceItemDto> BookingServices { get; set; } = new();
}

public class CreateBookingServiceItemDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Dịch vụ không hợp lệ.")]
    public int ServiceId { get; set; }

    [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000.")]
    public int Quantity { get; set; }
}
