using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.ViewModels;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers;

public class BookingsController : Controller
{
    private readonly ICourtApiService _apiService;

    public BookingsController(ICourtApiService apiService)
    {
        _apiService = apiService;
    }

    // GET: /Bookings/Create?courtId={courtId}&date={date}&slotId={slotId}
    [HttpGet]
    public async Task<IActionResult> Create(int courtId, string date, int slotId)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            parsedDate = DateOnly.FromDateTime(DateTime.Today);
        }

        var court = await _apiService.GetCourtDetailAsync(courtId);
        if (court == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sân đấu yêu cầu.";
            return RedirectToAction("Index", "Courts");
        }

        var availability = await _apiService.GetCourtAvailabilityAsync(courtId, parsedDate.ToDateTime(TimeOnly.MinValue));
        var slot = availability?.Slots?.FirstOrDefault(s => s.SlotId == slotId);

        // Fallback for mock if slot isn't found in API response
        if (slot == null)
        {
            // Create a mock slot based on the pricing configurations of the court
            var courtPricing = court.Pricings?.FirstOrDefault(p => p.SlotId == slotId);
            if (courtPricing != null)
            {
                var isWeekend = parsedDate.DayOfWeek == DayOfWeek.Saturday || parsedDate.DayOfWeek == DayOfWeek.Sunday;
                var basePrice = courtPricing.Price;
                var finalPrice = isWeekend ? basePrice * courtPricing.PeakMultiplier : basePrice;

                slot = new AvailabilitySlotDto
                {
                    SlotId = courtPricing.SlotId,
                    SlotName = courtPricing.SlotName,
                    StartTime = TimeSpan.TryParse(courtPricing.StartTime, out var st) ? st : TimeSpan.Zero,
                    EndTime = TimeSpan.TryParse(courtPricing.EndTime, out var et) ? et : TimeSpan.Zero,
                    Price = finalPrice,
                    Status = "Available"
                };
            }
            else
            {
                // Absolute fallback
                slot = new AvailabilitySlotDto
                {
                    SlotId = slotId,
                    SlotName = "Khung giờ " + slotId,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(9, 30, 0),
                    Price = 120000,
                    Status = "Available"
                };
            }
        }

        var token = GetToken();

        var viewModel = new CreateBookingViewModel
        {
            Court = court,
            BookingDate = parsedDate,
            SelectedSlot = slot,
            Token = token
        };

        return View(viewModel);
    }

    // POST: /Bookings/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int courtId, 
        string date, 
        int slotId,
        List<int>? slotIds, 
        string? note, 
        string? promotionCode, 
        int? racketQty, 
        int? drinkQty,
        bool isRecurring,
        string? startDate,
        string? endDate,
        List<int>? daysOfWeek)
    {
        var token = GetToken();

        if (isRecurring)
        {
            if (!DateOnly.TryParse(startDate, out var parsedStart)) parsedStart = DateOnly.FromDateTime(DateTime.Today);
            if (!DateOnly.TryParse(endDate, out var parsedEnd)) parsedEnd = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
            if (daysOfWeek == null || !daysOfWeek.Any()) daysOfWeek = new List<int> { 1, 3, 5 }; // default T2, T4, T6

            var recurringRequest = new RecurringBookingRequestDto
            {
                CourtId = courtId,
                SlotId = slotId,
                StartDate = parsedStart,
                EndDate = parsedEnd,
                DaysOfWeek = daysOfWeek,
                PromotionCode = promotionCode,
                Note = note
            };

            RecurringBookingResponseDto? recurringResponse = null;
            try
            {
                recurringResponse = await _apiService.CreateRecurringBookingAsync(recurringRequest, token);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Detail", "Courts", new { id = courtId });
            }

            // Fallback Mock recurring booking if API is offline
            if (recurringResponse == null)
            {
                var court = await _apiService.GetCourtDetailAsync(courtId);
                var availability = await _apiService.GetCourtAvailabilityAsync(courtId, parsedStart.ToDateTime(TimeOnly.MinValue));
                var slot = availability?.Slots?.FirstOrDefault(s => s.SlotId == slotId);
                
                decimal slotPrice = 120000;
                string slotName = "08:00 - 09:30";
                if (slot != null)
                {
                    slotPrice = slot.Price;
                    slotName = $"{slot.StartTime.ToString(@"hh\:mm")} - {slot.EndTime.ToString(@"hh\:mm")}";
                }
                else if (court != null)
                {
                    var pricing = court.Pricings?.FirstOrDefault(p => p.SlotId == slotId);
                    if (pricing != null)
                    {
                        slotPrice = pricing.Price;
                        slotName = $"{pricing.StartTime} - {pricing.EndTime}";
                    }
                }

                // Generate dates
                var allDates = new List<DateOnly>();
                for (var d = parsedStart; d <= parsedEnd; d = d.AddDays(1))
                {
                    if (daysOfWeek.Contains((int)d.DayOfWeek))
                        allDates.Add(d);
                }

                // Simulate 1 conflict date if we have at least 3 dates
                var conflictDates = new List<string>();
                var validDates = new List<DateOnly>(allDates);
                if (allDates.Count >= 3)
                {
                    var conflict = allDates[1];
                    conflictDates.Add(conflict.ToString("dd/MM/yyyy"));
                    validDates.Remove(conflict);
                }

                var createdBookings = new List<BookingDetailDto>();
                foreach (var validDate in validDates)
                {
                    createdBookings.Add(new BookingDetailDto
                    {
                        BookingId = new Random().Next(20000, 99999),
                        BookingCode = $"RBK{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100, 999)}",
                        CourtId = courtId,
                        CourtName = court?.CourtName ?? "Sân vận động",
                        SlotId = slotId,
                        SlotName = slotName,
                        BookingDate = validDate.ToDateTime(TimeOnly.MinValue),
                        SubTotal = slotPrice,
                        DiscountAmount = 0,
                        TotalAmount = slotPrice,
                        Status = "Pending",
                        CreatedAt = DateTime.Now
                    });
                }

                decimal totalEstimated = createdBookings.Sum(b => b.TotalAmount);
                if (!string.IsNullOrEmpty(promotionCode))
                {
                    totalEstimated = totalEstimated * 0.9m; // 10% discount
                }

                string daysDisplay = string.Join(", ", daysOfWeek.OrderBy(d => d).Select(d => d switch
                {
                    0 => "CN",
                    1 => "T2",
                    2 => "T3",
                    3 => "T4",
                    4 => "T5",
                    5 => "T6",
                    6 => "T7",
                    _ => d.ToString()
                }));

                recurringResponse = new RecurringBookingResponseDto
                {
                    RecurringId = new Random().Next(1000, 9999),
                    CourtId = courtId,
                    CourtName = court?.CourtName ?? "Sân vận động",
                    SlotId = slotId,
                    SlotName = slotName,
                    StartDate = parsedStart.ToDateTime(TimeOnly.MinValue),
                    EndDate = parsedEnd.ToDateTime(TimeOnly.MinValue),
                    DaysOfWeek = daysDisplay,
                    Status = "Active",
                    CreatedBookings = createdBookings,
                    ConflictDates = conflictDates,
                    TotalRequestedSessions = allDates.Count,
                    TotalBookedSessions = validDates.Count,
                    TotalEstimatedAmount = totalEstimated
                };
            }

            TempData["RecurringBookingResult"] = System.Text.Json.JsonSerializer.Serialize(recurringResponse);
            return RedirectToAction("RecurringSuccess");
        }
        else
        {
            if (!DateOnly.TryParse(date, out var parsedDate))
            {
                parsedDate = DateOnly.FromDateTime(DateTime.Today);
            }

            // Prepare request DTO
            var bookingRequest = new BookingRequestDto
            {
                CourtId = courtId,
                BookingDate = parsedDate,
                TimeSlotIds = new List<int> { slotId },
                Note = note,
                PromotionCode = promotionCode,
                Services = new List<BookingServiceRequestDto>()
            };

            if (racketQty.HasValue && racketQty.Value > 0)
            {
                bookingRequest.Services.Add(new BookingServiceRequestDto { ServiceId = 1, Quantity = racketQty.Value });
            }
            if (drinkQty.HasValue && drinkQty.Value > 0)
            {
                bookingRequest.Services.Add(new BookingServiceRequestDto { ServiceId = 4, Quantity = drinkQty.Value });
            }

            // Call API
            BookingResponseDto? bookingResponse = null;
            try
            {
                bookingResponse = await _apiService.CreateBookingAsync(bookingRequest, token);
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Detail", "Courts", new { id = courtId });
            }

            if (bookingResponse != null && (string.Equals(bookingResponse.Status, "Confirmed", StringComparison.OrdinalIgnoreCase) || string.Equals(bookingResponse.Status, "Paid", StringComparison.OrdinalIgnoreCase)))
            {
                TempData["SuccessMessage"] = $"Đặt sân thành công! Số tiền {bookingResponse.TotalAmount:N0}đ đã được thanh toán từ ví.";
                return RedirectToAction("PaymentSuccess", new { bookingId = bookingResponse.BookingId, paymentMethod = "Ví điện tử" });
            }

            // Fallback Mock booking if API is offline or returns error
            if (bookingResponse == null)
            {
                var court = await _apiService.GetCourtDetailAsync(courtId);
                var availability = await _apiService.GetCourtAvailabilityAsync(courtId, parsedDate.ToDateTime(TimeOnly.MinValue));
                var slot = availability?.Slots?.FirstOrDefault(s => s.SlotId == slotId);
                
                decimal slotPrice = 120000;
                string slotName = "08:00 - 09:30";
                if (slot != null)
                {
                    slotPrice = slot.Price;
                    slotName = $"{slot.StartTime.ToString(@"hh\:mm")} - {slot.EndTime.ToString(@"hh\:mm")}";
                }
                else if (court != null)
                {
                    var pricing = court.Pricings?.FirstOrDefault(p => p.SlotId == slotId);
                    if (pricing != null)
                    {
                        var isWeekend = parsedDate.DayOfWeek == DayOfWeek.Saturday || parsedDate.DayOfWeek == DayOfWeek.Sunday;
                        var basePrice = pricing.Price;
                        slotPrice = isWeekend ? basePrice * pricing.PeakMultiplier : basePrice;
                        slotName = $"{pricing.StartTime} - {pricing.EndTime}";
                    }
                }

                decimal servicesAmount = 0;
                if (racketQty.HasValue) servicesAmount += racketQty.Value * 30000; // Mock: 30k per racket
                if (drinkQty.HasValue) servicesAmount += drinkQty.Value * 15000;  // Mock: 15k per drink

                decimal discountAmount = 0;
                if (!string.IsNullOrEmpty(promotionCode))
                {
                    discountAmount = (slotPrice + servicesAmount) * 0.1m; // Mock 10% discount
                }

                bookingResponse = new BookingResponseDto
                {
                    BookingId = new Random().Next(20000, 99999),
                    CourtName = court?.CourtName ?? "Sân vận động",
                    BookingDate = parsedDate,
                    Slots = new List<BookingSlotResponseDto>
                    {
                        new BookingSlotResponseDto
                        {
                            StartTime = slotName.Split(" - ")[0],
                            EndTime = slotName.Split(" - ")[1]
                        }
                    },
                    SubTotalAmount = slotPrice,
                    ServicesAmount = servicesAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = slotPrice + servicesAmount - discountAmount,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                // Save in TempData as a mock database simulation
                TempData[$"MockBooking_{bookingResponse.BookingId}"] = System.Text.Json.JsonSerializer.Serialize(bookingResponse);
            }

            return RedirectToAction("Payment", new { bookingId = bookingResponse.BookingId });
        }
    }

    // GET: /Bookings/RecurringSuccess
    [HttpGet]
    public IActionResult RecurringSuccess()
    {
        if (TempData.TryGetValue("RecurringBookingResult", out var resultObj))
        {
            var json = resultObj as string;
            if (!string.IsNullOrEmpty(json))
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<RecurringBookingResponseDto>(json);
                return View(result);
            }
        }
        
        TempData["ErrorMessage"] = "Không tìm thấy kết quả đặt sân định kỳ.";
        return RedirectToAction("Index", "Courts");
    }

    // GET: /Bookings/Payment?bookingId={bookingId}
    [HttpGet]
    public async Task<IActionResult> Payment(int bookingId)
    {
        var token = GetToken();
        
        // Try getting from API
        var booking = await _apiService.GetBookingDetailAsync(bookingId, token);

        // Fallback: Check mock DB in TempData
        if (booking == null && TempData.TryGetValue($"MockBooking_{bookingId}", out var mockJsonObj))
        {
            var mockJson = mockJsonObj as string;
            if (!string.IsNullOrEmpty(mockJson))
            {
                booking = System.Text.Json.JsonSerializer.Deserialize<BookingResponseDto>(mockJson);
                // Keep it in TempData for the next requests
                TempData.Keep($"MockBooking_{bookingId}");
            }
        }

        if (booking == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt sân.";
            return RedirectToAction("Index", "Courts");
        }

        return View(booking);
    }

    // POST: /Bookings/Payment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(int bookingId, string paymentMethod)
    {
        // Bypass third-party payment gateways since online payment is under maintenance
        TempData["SuccessMessage"] = "Đã xác nhận hình thức thanh toán tại quầy!";
        return RedirectToAction("PaymentSuccess", new { bookingId = bookingId, paymentMethod = "Thanh toán tại quầy" });
    }

    // GET: /Bookings/PaymentSuccess?bookingId={bookingId}&paymentMethod={paymentMethod}
    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(int bookingId, string paymentMethod)
    {
        var token = GetToken();
        var booking = await _apiService.GetBookingDetailAsync(bookingId, token);

        if (booking == null && TempData.TryGetValue($"MockBooking_{bookingId}", out var mockJsonObj))
        {
            var mockJson = mockJsonObj as string;
            if (!string.IsNullOrEmpty(mockJson))
            {
                booking = System.Text.Json.JsonSerializer.Deserialize<BookingResponseDto>(mockJson);
                TempData.Keep($"MockBooking_{bookingId}");
            }
        }

        if (booking == null)
        {
            booking = new BookingResponseDto
            {
                BookingId = bookingId,
                CourtName = "Sân đấu thể thao",
                BookingDate = DateOnly.FromDateTime(DateTime.Today),
                TotalAmount = 120000,
                Status = "Paid"
            };
        }
        else
        {
            booking.Status = "Paid"; // Update status to Paid for success screen
        }

        ViewBag.PaymentMethod = paymentMethod ?? "Cổng thanh toán";
        return View(booking);
    }

    private string? GetToken()
    {
        var token = HttpContext.Session.GetString(Services.Api.JwtForwardingHandler.SessionTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            token = User.FindFirst(Services.Api.JwtForwardingHandler.AccessTokenClaimType)?.Value;
        }
        return token;
    }

    // GET: /Bookings/MyBookings
    [HttpGet]
    public async Task<IActionResult> MyBookings(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int page = 1)
    {
        var token = GetToken();
        var pagedData = await _apiService.GetPagedMyBookingsAsync(keyword, fromDate, toDate, status, page, 10, token);
        var vm = new BookingListViewModel
        {
            PagedData = pagedData,
            Keyword = keyword,
            FromDate = fromDate,
            ToDate = toDate,
            Status = status
        };
        return View(vm);
    }

    // GET: /Bookings/AdminIndex
    [HttpGet]
    public async Task<IActionResult> AdminIndex(string? keyword, DateTime? fromDate, DateTime? toDate, int? courtTypeId, string? status, int page = 1)
    {
        var pagedData = await _apiService.GetPagedAdminBookingsAsync(keyword, fromDate, toDate, courtTypeId, status, page, 15);
        var courtTypes = await _apiService.GetCourtTypesAsync();
        ViewBag.CourtTypes = courtTypes;

        var vm = new BookingListViewModel
        {
            PagedData = pagedData,
            Keyword = keyword,
            FromDate = fromDate,
            ToDate = toDate,
            CourtTypeId = courtTypeId,
            Status = status
        };
        return View(vm);
    }

    // POST: /Bookings/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? cancelReason, string returnUrl)
    {
        var success = await _apiService.UpdateBookingStatusAsync(id, status, cancelReason);
        if (success) TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn đặt sân #{id} thành công!";
        else TempData["ErrorMessage"] = $"Lỗi khi cập nhật trạng thái đơn #{id}.";

        if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) && referer.Contains("AdminIndex", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(AdminIndex));
        }
        return RedirectToAction(nameof(MyBookings));
    }
}
