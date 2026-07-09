using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Bookings;
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

        // POST: /Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int courtId,
            int slotId,
            string bookingDate,
            string? promoCode,
            Dictionary<int, int>? serviceQuantities)
        {
            if (!DateTime.TryParse(bookingDate, out var parsedDate))
            {
                TempData["ErrorMessage"] = "Ngày đặt sân không hợp lệ.";
                return RedirectToAction("Index");
            }

            var request = new CreateBookingRequestDto
            {
                CourtId = courtId,
                SlotId = slotId,
                BookingDate = parsedDate,
                PromoCode = promoCode,
                BookingServices = new List<CreateBookingServiceItemDto>()
            };

            // Add selected services
            if (serviceQuantities != null)
            {
                foreach (var kvp in serviceQuantities)
                {
                    if (kvp.Value > 0)
                    {
                        request.BookingServices.Add(new CreateBookingServiceItemDto
                        {
                            ServiceId = kvp.Key,
                            Quantity = kvp.Value
                        });
                    }
                }
            }

            try
            {
                var result = await _apiService.CreateSingularBookingAsync(request);
                if (result != null)
                {
                    // Store in TempData for the payment page
                    TempData["BookingResponse"] = JsonSerializer.Serialize(result);
                    return RedirectToAction("Payment", new { bookingCode = result.BookingCode });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tạo đặt sân. Vui lòng thử lại.";
                    return RedirectToAction("Index", new { courtId, date = bookingDate, slotId });
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", new { courtId, date = bookingDate, slotId });
            }
        }

        // GET: /Booking/Payment?bookingCode=BK-XXXXXXXX
        [HttpGet]
        public async Task<IActionResult> Payment(string bookingCode)
        {
            SingularBookingResponseDto? booking = null;

            // Try to get from TempData first (just created)
            if (TempData.TryGetValue("BookingResponse", out var bookingJson))
            {
                var json = bookingJson as string;
                if (!string.IsNullOrEmpty(json))
                {
                    booking = JsonSerializer.Deserialize<SingularBookingResponseDto>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }

            if (booking == null || booking.BookingCode != bookingCode)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt sân. Phiên có thể đã hết hạn.";
                return RedirectToAction("Index");
            }

            // Get QR code from SePay API
            var qrCode = await _apiService.GetSePayQrCodeAsync(bookingCode);

            ViewBag.QrCode = qrCode;
            return View(booking);
        }

        // POST: /Booking/SimulatePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulatePayment(string bookingCode, decimal amount)
        {
            var (success, message, _) = await _apiService.SimulateSePayWebhookAsync(bookingCode, amount);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction("Success", new { bookingCode });
            }
            else
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction("Index");
            }
        }

        // GET: /Booking/Success?bookingCode=BK-XXXXXXXX
        [HttpGet]
        public IActionResult Success(string bookingCode)
        {
            ViewBag.BookingCode = bookingCode;
            return View();
        }
    }
}
