using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SportCourtManagement_FrontEnd.Models.Api;

namespace SportCourtManagement_FrontEnd.Models.Tournaments;

/// <summary>
/// Tournament detail DTO.
/// </summary>
public class TournamentDto
{
  public int TournamentId { get; set; }
  public string TournamentName { get; set; } = string.Empty;
  public string? Description { get; set; }
  public int UserId { get; set; }
  public string CustomerName { get; set; } = string.Empty;
  public decimal TotalAmount { get; set; }
  public string Status { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public List<TournamentBookingItemDto> Bookings { get; set; } = new();
}

public class TournamentBookingServiceItemDto
{
  public int ServiceId { get; set; }
  public string ServiceName { get; set; } = string.Empty;
  public int Quantity { get; set; }
  public decimal UnitPrice { get; set; }
  public decimal TotalPrice { get; set; }
}

public class TournamentBookingItemDto
{
  public int BookingId { get; set; }
  public string BookingCode { get; set; } = string.Empty;
  public int CourtId { get; set; }
  public string CourtName { get; set; } = string.Empty;
  public string SlotName { get; set; } = string.Empty;
  public DateTime BookingDate { get; set; }
  public string StartTime { get; set; } = string.Empty;
  public string EndTime { get; set; } = string.Empty;
  public decimal TotalAmount { get; set; }
  public string Status { get; set; } = string.Empty;
  public List<TournamentBookingServiceItemDto> Services { get; set; } = new();
}

/// <summary>
/// Public tournament DTO for customer viewing.
/// </summary>
public class TournamentPublicDto
{
  public int TournamentId { get; set; }
  public string TournamentName { get; set; } = string.Empty;
  public string? Description { get; set; }
  public string OrganizerName { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public List<CourtSlotPublicDto> Courts { get; set; } = new();
}

public class CourtSlotPublicDto
{
  public int CourtId { get; set; }
  public string CourtName { get; set; } = string.Empty;
  public int SlotId { get; set; }
  public string SlotName { get; set; } = string.Empty;
  public string StartTime { get; set; } = string.Empty;
  public string EndTime { get; set; } = string.Empty;
  public DateTime BookingDate { get; set; }
  public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Form creation request for tournament.
/// </summary>
public class CreateTournamentFormDto
{
  [Required(ErrorMessage = "Vui lòng nhập tên giải đấu")]
  public string TournamentName { get; set; } = string.Empty;

  public string? Description { get; set; }

  [Required(ErrorMessage = "Vui lòng chọn ngày tổ chức")]
  public DateTime BookingDate { get; set; } = DateTime.Today.AddDays(7);

  public string? PromotionCode { get; set; }
  public string? Note { get; set; }

  [Required(ErrorMessage = "Vui lòng chọn ít nhất một sân")]
  [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một sân")]
  public List<TournamentCourtSelectionDto> CourtSelections { get; set; } = new();

  public List<TournamentServiceSelectionDto>? Services { get; set; } = new();
}

public class TournamentCourtSelectionDto
{
  [Range(1, int.MaxValue, ErrorMessage = "ID sân không hợp lệ")]
  public int CourtId { get; set; }

  [Required]
  [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một khung giờ")]
  public List<int> SlotIds { get; set; } = new();
}

public class TournamentServiceSelectionDto
{
  public int ServiceId { get; set; }
  public int Quantity { get; set; }
}

/// <summary>
/// View model for tournaments list page.
/// </summary>
public class TournamentListViewModel
{
  public PagedResult<TournamentDto> PagedData { get; set; } = new();
  public string? Keyword { get; set; }
  public DateTime? FromDate { get; set; }
  public DateTime? ToDate { get; set; }
  public string? Status { get; set; }
}

/// <summary>
/// View model for public tournaments list page.
/// </summary>
public class PublicTournamentListViewModel
{
  public PagedResult<TournamentPublicDto> PagedData { get; set; } = new();
  public string? Keyword { get; set; }
}
