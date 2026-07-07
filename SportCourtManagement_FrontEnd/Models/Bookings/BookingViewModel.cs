using System;
using System.Collections.Generic;
using SportCourtManagement_FrontEnd.Models.Api;

namespace SportCourtManagement_FrontEnd.Models.Bookings;

/// <summary>
/// Full booking DTO matching server response.
/// </summary>
public class BookingDetailDto
{
  public int BookingId { get; set; }
  public string BookingCode { get; set; } = string.Empty;
  public int UserId { get; set; }
  public string CustomerName { get; set; } = string.Empty;
  public string? CustomerPhone { get; set; }
  public int CourtId { get; set; }
  public string CourtName { get; set; } = string.Empty;
  public int SlotId { get; set; }
  public string SlotName { get; set; } = string.Empty;
  public DateTime BookingDate { get; set; }
  public string StartTime { get; set; } = string.Empty;
  public string EndTime { get; set; } = string.Empty;
  public decimal SubTotal { get; set; }
  public decimal DiscountAmount { get; set; }
  public decimal TotalAmount { get; set; }
  public string Status { get; set; } = string.Empty;
  public int? PromotionId { get; set; }
  public string? PromotionCode { get; set; }
  public string? Note { get; set; }
  public string? CancelReason { get; set; }
  public DateTime CreatedAt { get; set; }
}

/// <summary>
/// View model for booking lists.
/// </summary>
public class BookingListViewModel
{
  public PagedResult<BookingDetailDto> PagedData { get; set; } = new();
  public string? Keyword { get; set; }
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
  public int? CourtTypeId { get; set; }
  public string? Status { get; set; }
}
