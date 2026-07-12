using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.ViewModels;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers
{
    public class BookingController : Controller
    {
        private readonly ICourtApiService _apiService;

        public BookingController(ICourtApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /Booking?courtId=1&date=2026-07-12&slotId=3
        [HttpGet]
        public async Task<IActionResult> Index(int? courtId, string? date, int? slotId)
        {
            // Load courts list
            var courtsResult = await _apiService.SearchCourtsAsync(new Models.Courts.CourtSearchParams
            {
                PageNumber = 1,
                PageSize = 50,
                Status = "Active"
            });

            // Load time slots
            var timeSlots = await _apiService.GetTimeSlotsAsync();

            // Load services
            var services = await _apiService.GetServicesAsync();

            DateTime? selectedDate = null;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
            {
                selectedDate = parsedDate;
            }

            var viewModel = new SingularBookingViewModel
            {
                Courts = courtsResult.Items?.ToList() ?? new(),
                TimeSlots = timeSlots,
                Services = services,
                SelectedCourtId = courtId,
                SelectedDate = selectedDate,
                SelectedSlotId = slotId
            };

            return View(viewModel);
        }

        // AJAX: /Booking/GetSlotAvailability?courtId=1&date=2026-07-12
        [HttpGet]
        public async Task<IActionResult> GetSlotAvailability(int courtId, string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return Json(new { success = false, message = "Ngày không hợp lệ." });
            }

            // Get all defined time slots
            var allTimeSlots = await _apiService.GetTimeSlotsAsync();

            // Get court-specific availability for this date
            var availability = await _apiService.GetCourtAvailabilityAsync(courtId, parsedDate);

            // Get court detail for pricing info
            var court = await _apiService.GetCourtDetailAsync(courtId);

            // Merge: start from allTimeSlots, overlay availability status
            var result = allTimeSlots.Select(ts =>
            {
                var availSlot = availability?.Slots?.FirstOrDefault(s => s.SlotId == ts.SlotId);
                var pricing = court?.Pricings?.FirstOrDefault(p => p.SlotId == ts.SlotId);

                string status = "Available";
                decimal price = 0;

                if (availSlot != null)
                {
                    status = availSlot.Status; // Available | Booked | Maintenance
                    price = availSlot.Price;
                }
                else if (pricing != null)
                {
                    var isWeekend = parsedDate.DayOfWeek == DayOfWeek.Saturday || parsedDate.DayOfWeek == DayOfWeek.Sunday;
                    price = isWeekend ? pricing.Price * pricing.PeakMultiplier : pricing.Price;
                }

                return new
                {
                    slotId = ts.SlotId,
                    slotName = ts.SlotName,
                    startTime = ts.StartTime,
                    endTime = ts.EndTime,
                    price = price,
                    status = status
                };
            }).ToList();

            return Json(new { success = true, slots = result });
        }

        // POST: /Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int courtId,
            List<int> slotIds,
            string bookingDate,
            string? promoCode,
            Dictionary<int, int>? serviceQuantities)
        {
            if (!DateTime.TryParse(bookingDate, out var parsedDate))
            {
                TempData["ErrorMessage"] = "Ngày đặt sân không hợp lệ.";
                return RedirectToAction("Index");
            }

            if (slotIds == null || slotIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một khung giờ.";
                return RedirectToAction("Index", new { courtId, date = bookingDate });
            }

            // Build service items list once (shared across all slot bookings)
            var serviceItems = new List<CreateBookingServiceItemDto>();
            if (serviceQuantities != null)
            {
                foreach (var kvp in serviceQuantities)
                {
                    if (kvp.Value > 0)
                    {
                        serviceItems.Add(new CreateBookingServiceItemDto
                        {
                            ServiceId = kvp.Key,
                            Quantity = kvp.Value
                        });
                    }
                }
            }

            var successfulBookings = new List<SingularBookingResponseDto>();
            var errors = new List<string>();

            // Create one booking per selected slot
            foreach (var slotId in slotIds)
            {
                var request = new CreateBookingRequestDto
                {
                    CourtId = courtId,
                    SlotId = slotId,
                    BookingDate = parsedDate,
                    PromoCode = promoCode,
                    BookingServices = serviceItems
                };

                try
                {
                    var result = await _apiService.CreateSingularBookingAsync(request);
                    if (result != null)
                    {
                        successfulBookings.Add(result);
                    }
                    else
                    {
                        errors.Add($"Slot {slotId}: Không thể tạo đặt sân.");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    errors.Add($"Slot {slotId}: {ex.Message}");
                }
            }

            if (successfulBookings.Count == 0)
            {
                TempData["ErrorMessage"] = "Không thể tạo đặt sân. " + string.Join(" | ", errors);
                return RedirectToAction("Index", new { courtId, date = bookingDate });
            }

            if (errors.Count > 0)
            {
                TempData["WarningMessage"] = $"Đã tạo {successfulBookings.Count}/{slotIds.Count} đơn. Lỗi: {string.Join(" | ", errors)}";
            }

            // Store all bookings in TempData for the payment page
            TempData["BookingResponses"] = JsonSerializer.Serialize(successfulBookings);
            var bookingCodes = string.Join(",", successfulBookings.Select(b => b.BookingCode));
            return RedirectToAction("Payment", new { bookingCodes });
        }

        // GET: /Booking/Payment?bookingCodes=BK-001,BK-002
        [HttpGet]
        public async Task<IActionResult> Payment(string bookingCodes)
        {
            var bookings = new List<SingularBookingResponseDto>();

            // Try to get from TempData first (just created)
            if (TempData.TryGetValue("BookingResponses", out var bookingJson))
            {
                var json = bookingJson as string;
                if (!string.IsNullOrEmpty(json))
                {
                    bookings = JsonSerializer.Deserialize<List<SingularBookingResponseDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new();
                }
            }

            if (bookings.Count == 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt sân. Phiên có thể đã hết hạn.";
                return RedirectToAction("Index");
            }

            // Calculate total amount across all bookings
            var totalAmount = bookings.Sum(b => b.TotalAmount);
            ViewBag.TotalAmount = totalAmount;
            ViewBag.BookingCodes = bookingCodes;

            // Get QR code for the first booking (as reference)
            var qrCode = await _apiService.GetSePayQrCodeAsync(bookings.First().BookingCode);
            ViewBag.QrCode = qrCode;

            return View(bookings);
        }

        // POST: /Booking/SimulatePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulatePayment(string bookingCodes)
        {
            if (string.IsNullOrEmpty(bookingCodes))
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã đặt sân.";
                return RedirectToAction("Index");
            }

            var codes = bookingCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var successCount = 0;
            var lastMessage = "";

            foreach (var code in codes)
            {
                // We pass amount = 0, the backend webhook handler will validate the booking's actual amount
                var (success, message, _) = await _apiService.SimulateSePayWebhookAsync(code.Trim(), 0);
                if (success)
                {
                    successCount++;
                    lastMessage = message;
                }
            }

            if (successCount > 0)
            {
                TempData["SuccessMessage"] = $"Đã thanh toán thành công {successCount}/{codes.Length} đơn đặt sân.";
                return RedirectToAction("Success", new { bookingCodes });
            }
            else
            {
                TempData["ErrorMessage"] = "Thanh toán không thành công. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Booking/Success?bookingCodes=BK-001,BK-002
        [HttpGet]
        public IActionResult Success(string bookingCodes)
        {
            ViewBag.BookingCode = bookingCodes;
            ViewBag.BookingCodeList = bookingCodes?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            return View();
        }

        // GET: /Booking/CheckStatus?bookingCodes=BK-001,BK-002
        [HttpGet]
        public async Task<IActionResult> CheckStatus(string bookingCodes)
        {
            if (string.IsNullOrEmpty(bookingCodes))
            {
                return Json(new { success = false, message = "Không tìm thấy mã đặt sân." });
            }

            var codes = bookingCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var allPaid = true;

            foreach (var code in codes)
            {
                try
                {
                    var jsonStr = await _apiService.GetRawJsonAsync($"api/SePay/status/{code.Trim()}");
                    using var statusDoc = JsonDocument.Parse(jsonStr);
                    if (statusDoc.RootElement.TryGetProperty("status", out var statusProp))
                    {
                        var status = statusProp.GetString();
                        // If it is in DB, status will be "Confirmed" or "Paid" (indicating paid)
                        if (status != "Confirmed" && status != "Paid")
                        {
                            allPaid = false;
                            break;
                        }
                    }
                    else
                    {
                        allPaid = false;
                        break;
                    }
                }
                catch
                {
                    allPaid = false;
                    break;
                }
            }

            return Json(new { success = true, allPaid = allPaid });
        }
    }
}
