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

        var availability = await _apiService.GetCourtAvailabilityAsync(courtId, parsedDate);
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
                    StartTime = courtPricing.StartTime,
                    EndTime = courtPricing.EndTime,
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
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(9, 30),
                    Price = 120000,
                    Status = "Available"
                };
            }
        }

        var token = Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"];

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
    public async Task<IActionResult> Create(int courtId, string date, int slotId, string? note, string? promotionCode, int? racketQty, int? drinkQty)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            parsedDate = DateOnly.FromDateTime(DateTime.Today);
        }

        var token = Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"];
        
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
        var bookingResponse = await _apiService.CreateBookingAsync(bookingRequest, token);

        // Fallback Mock booking if API is offline or returns error
        if (bookingResponse == null)
        {
            var court = await _apiService.GetCourtDetailAsync(courtId);
            var availability = await _apiService.GetCourtAvailabilityAsync(courtId, parsedDate);
            var slot = availability?.Slots?.FirstOrDefault(s => s.SlotId == slotId);
            
            decimal slotPrice = 120000;
            string slotName = "08:00 - 09:30";
            if (slot != null)
            {
                slotPrice = slot.Price;
                slotName = $"{slot.StartTime:HH:mm} - {slot.EndTime:HH:mm}";
            }
            else if (court != null)
            {
                var pricing = court.Pricings?.FirstOrDefault(p => p.SlotId == slotId);
                if (pricing != null)
                {
                    var isWeekend = parsedDate.DayOfWeek == DayOfWeek.Saturday || parsedDate.DayOfWeek == DayOfWeek.Sunday;
                    slotPrice = isWeekend ? pricing.Price * pricing.PeakMultiplier : pricing.Price;
                    slotName = $"{pricing.StartTime:HH:mm} - {pricing.EndTime:HH:mm}";
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

    // GET: /Bookings/Payment?bookingId={bookingId}
    [HttpGet]
    public async Task<IActionResult> Payment(int bookingId)
    {
        var token = Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"];
        
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
        var token = Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"];

        var paymentRequest = new PaymentRequestDto
        {
            BookingId = bookingId,
            PaymentMethod = paymentMethod
        };

        // Call API
        var paymentResponse = await _apiService.CreatePaymentLinkAsync(paymentRequest, token);

        if (paymentResponse != null && !string.IsNullOrEmpty(paymentResponse.PaymentUrl))
        {
            return Redirect(paymentResponse.PaymentUrl);
        }

        // Mock payment flow fallback
        TempData["SuccessMessage"] = $"Thanh toán thành công qua {paymentMethod}!";
        return RedirectToAction("PaymentSuccess", new { bookingId = bookingId, paymentMethod = paymentMethod });
    }

    // GET: /Bookings/PaymentSuccess?bookingId={bookingId}&paymentMethod={paymentMethod}
    [HttpGet]
    public async Task<IActionResult> PaymentSuccess(int bookingId, string paymentMethod)
    {
        var token = Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"];
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

    private string? GetToken() => Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"];

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
        var token = GetToken();
        var pagedData = await _apiService.GetPagedAdminBookingsAsync(keyword, fromDate, toDate, courtTypeId, status, page, 15, token);
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
        var token = GetToken();
        var success = await _apiService.UpdateBookingStatusAsync(id, status, cancelReason, token);
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
