using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.ViewModels;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers
{
    public class BookingController : Controller
    {
        private readonly ICourtApiService _apiService;
        private readonly ILogger<BookingController> _logger;

        public BookingController(ICourtApiService apiService, ILogger<BookingController> logger)
        {
            _apiService = apiService;
            _logger = logger;
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

                // Never advertise a slot as available when the backend omitted it
                // or the availability request failed.
                string status = availability == null ? "Error" : "Inactive";
                decimal price = 0;

                if (availSlot != null)
                {
                    status = availSlot.Status; // Available | Held | Booked | Maintenance | Inactive
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

        // AJAX: /Booking/GetCourtServices?courtId=1
        [HttpGet]
        public async Task<IActionResult> GetCourtServices(int courtId)
        {
            var services = await _apiService.GetServicesByCourtIdAsync(courtId);
            return Json(new { success = true, services = services });
        }

        // POST: /Booking/CreateRecurring
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRecurring(
            int courtId,
            int? slotId,
            List<int>? slotIds,
            string startDate,
            string endDate,
            List<int> daysOfWeek,
            string? promoCode,
            string? note)
        {
            var targetSlots = slotIds ?? new List<int>();
            if (targetSlots.Count == 0 && slotId.HasValue)
            {
                targetSlots.Add(slotId.Value);
            }

            if (targetSlots.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một khung giờ.";
                return RedirectToAction("Index", new { courtId });
            }

            if (!DateTime.TryParse(startDate, out var parsedStartDate) || !DateTime.TryParse(endDate, out var parsedEndDate))
            {
                TempData["ErrorMessage"] = "Ngày bắt đầu hoặc ngày kết thúc không hợp lệ.";
                return RedirectToAction("Index", new { courtId });
            }

            if (parsedStartDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Ngày bắt đầu không được nằm trong quá khứ.";
                return RedirectToAction("Index", new { courtId });
            }

            if (parsedEndDate.Date <= parsedStartDate.Date)
            {
                TempData["ErrorMessage"] = "Ngày kết thúc phải sau ngày bắt đầu.";
                return RedirectToAction("Index", new { courtId });
            }

            if (daysOfWeek == null || daysOfWeek.Count == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một ngày trong tuần.";
                return RedirectToAction("Index", new { courtId });
            }

            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để thực hiện đặt sân định kỳ.";
                return RedirectToAction("Index", new { courtId });
            }

            var allCreatedBookings = new List<SingularBookingResponseDto>();
            var allConflictDates = new List<string>();

            try
            {
                foreach (var currentSlotId in targetSlots)
                {
                    var request = new RecurringBookingRequestDto
                    {
                        CourtId = courtId,
                        SlotId = currentSlotId,
                        StartDate = DateOnly.FromDateTime(parsedStartDate),
                        EndDate = DateOnly.FromDateTime(parsedEndDate),
                        DaysOfWeek = daysOfWeek,
                        PromotionCode = promoCode,
                        Note = note
                    };

                    var result = await _apiService.CreateRecurringBookingAsync(request, token);
                    if (result != null)
                    {
                        if (result.ConflictDates != null && result.ConflictDates.Count > 0)
                        {
                            allConflictDates.AddRange(result.ConflictDates);
                        }

                        if (result.CreatedBookings != null && result.CreatedBookings.Count > 0)
                        {
                            allCreatedBookings.AddRange(result.CreatedBookings.Select(b => new SingularBookingResponseDto
                            {
                                BookingId = b.BookingId,
                                BookingCode = b.BookingCode,
                                CourtId = b.CourtId,
                                CourtName = b.CourtName ?? result.CourtName,
                                SlotId = b.SlotId,
                                SlotName = b.SlotName ?? result.SlotName,
                                BookingDate = b.BookingDate,
                                SubTotal = b.SubTotal,
                                DiscountAmount = b.DiscountAmount,
                                TotalAmount = b.TotalAmount,
                                Status = b.Status
                            }));
                        }
                    }
                }

                if (allCreatedBookings.Count > 0)
                {
                    TempData["SuccessMessage"] = $"Đặt sân định kỳ thành công! Tổng số buổi đã đặt: {allCreatedBookings.Count}.";
                    if (allConflictDates.Count > 0)
                    {
                        TempData["WarningMessage"] = $"Các ngày bị trùng lịch đã được bỏ qua: {string.Join(", ", allConflictDates.Distinct())}.";
                    }

                    var bookingCodes = string.Join(",", allCreatedBookings.Select(b => b.BookingCode));
                    return RedirectToAction("Payment", new { bookingCodes });
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tạo lịch định kỳ. Có thể tất cả các buổi đều bị trùng lịch hoặc dịch vụ phản hồi không thành công.";
                }
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi tạo đặt sân định kỳ: " + ex.Message;
            }

            return RedirectToAction("Index", new { courtId });
        }

        // POST: /Booking/JoinWaitlist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinWaitlist(int courtId, int slotId, string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return Json(new { success = false, message = "Ngày không hợp lệ." });
            }

            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để đăng ký hàng chờ." });
            }

            var (success, message, position) = await _apiService.JoinWaitlistAsync(courtId, slotId, parsedDate, token);
            return Json(new { success, message, position });
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

            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đặt sân.";
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
                    var result = await _apiService.CreateSingularBookingAsync(request, token);
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
                TempData["ErrorMessage"] = "Không thể đặt sân. " + string.Join(" | ", errors);
                return RedirectToAction("Index", new { courtId, date = bookingDate });
            }

            if (errors.Count > 0)
            {
                TempData["WarningMessage"] = $"Đã đặt {successfulBookings.Count}/{slotIds.Count} sân thành công. Lỗi: {string.Join(" | ", errors)}";
            }

            var bookingCodes = string.Join(",", successfulBookings.Select(b => b.BookingCode));

            // Nếu đã được xác nhận (thanh toán qua ví thành công trực tiếp)
            if (successfulBookings.Any(b => string.Equals(b.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)))
            {
                TempData["SuccessMessage"] = $"Đặt sân thành công! Tổng cộng {successfulBookings.Sum(b => b.TotalAmount):N0}đ đã được thanh toán từ ví.";
                return RedirectToAction("Success", new { bookingCodes });
            }

            // Fallback
            TempData["BookingResponses"] = JsonSerializer.Serialize(successfulBookings);
            return RedirectToAction("Payment", new { bookingCodes });
        }

        // GET: /Booking/Payment?bookingCodes=BK-001,BK-002&isServicePayment=true
        [HttpGet]
        public async Task<IActionResult> Payment(string bookingCodes, bool isServicePayment = false)
        {
            var token = GetToken();
            var wallet = await _apiService.GetWalletBalanceAsync(token);
            ViewBag.Wallet = wallet;

            ViewBag.IsServicePayment = isServicePayment;
            ViewBag.BookingCodes = bookingCodes;

            if (isServicePayment && TempData.TryGetValue("ServicePaymentInfo", out var serviceInfoJson))
            {
                TempData.Keep("ServicePaymentInfo");
                var json = serviceInfoJson as string;
                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        var addAmount = root.GetProperty("AdditionalAmount").GetDecimal();
                        var addedText = root.GetProperty("AddedServicesText").GetString();
                        var servicesJson = root.GetProperty("Services").GetRawText();
                        var servicesList = JsonSerializer.Deserialize<List<SingularBookingServiceResponseDto>>(servicesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

                        ViewBag.TotalAmount = addAmount;
                        ViewBag.AddedServicesText = addedText;

                        var servicePaymentBooking = new SingularBookingResponseDto
                        {
                            BookingCode = bookingCodes,
                            TotalAmount = addAmount,
                            SubTotal = addAmount,
                            ServicesAmount = addAmount,
                            BookingServices = servicesList,
                            Note = $"Thanh toán dịch vụ mua thêm: {addedText}"
                        };

                        var qrCodeSvg = await _apiService.GetSePayQrCodeAsync(bookingCodes);
                        ViewBag.QrCode = qrCodeSvg;

                        return View(new List<SingularBookingResponseDto> { servicePaymentBooking });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing ServicePaymentInfo in Payment action");
                    }
                }
            }

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

            if (bookings.Count == 0 && !string.IsNullOrEmpty(bookingCodes))
            {
                var codes = bookingCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToList();
                var allMyBookings = await _apiService.GetPagedMyBookingsAsync(null, null, null, null, 1, 100, token);
                if (allMyBookings?.Items != null)
                {
                    foreach (var code in codes)
                    {
                        var matched = allMyBookings.Items.FirstOrDefault(b => string.Equals(b.BookingCode, code, StringComparison.OrdinalIgnoreCase));
                        if (matched != null)
                        {
                            bookings.Add(new SingularBookingResponseDto
                            {
                                BookingId = matched.BookingId,
                                BookingCode = matched.BookingCode,
                                UserId = matched.UserId,
                                CourtId = matched.CourtId,
                                CourtName = matched.CourtName,
                                SlotId = matched.SlotId,
                                SlotName = matched.SlotName,
                                BookingDate = matched.BookingDate,
                                StartTime = TimeSpan.Parse(matched.StartTime),
                                EndTime = TimeSpan.Parse(matched.EndTime),
                                SubTotal = matched.SubTotal,
                                DiscountAmount = matched.DiscountAmount,
                                TotalAmount = matched.TotalAmount,
                                Status = matched.Status,
                                BookingServices = matched.Services.Select(s => new SingularBookingServiceResponseDto
                                {
                                    ServiceId = s.ServiceId,
                                    ServiceName = s.ServiceName,
                                    Quantity = s.Quantity,
                                    Price = s.UnitPrice,
                                    TotalPrice = s.TotalPrice
                                }).ToList()
                            });
                        }
                    }
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

            // Get QR code for the first booking (as reference)
            var qrCode = await _apiService.GetSePayQrCodeAsync(bookings.First().BookingCode);
            ViewBag.QrCode = qrCode;

            return View(bookings);
        }

        // POST: /Booking/SimulatePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulatePayment(string bookingCodes, bool isServicePayment = false)
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
                if (isServicePayment)
                {
                    TempData["SuccessMessage"] = "Đã gửi SePay Webhook giả lập và xác nhận thanh toán dịch vụ bổ sung thành công!";
                    return RedirectToAction("Index", "MyBookings");
                }

                TempData["SuccessMessage"] = $"Đã gửi SePay Webhook giả lập thanh toán thành công {successCount}/{codes.Length} đơn đặt sân.";
                return RedirectToAction("Success", new { bookingCodes });
            }
            else
            {
                TempData["ErrorMessage"] = $"Giả lập Webhook thanh toán thất bại: {lastMessage}";
                return RedirectToAction("Payment", new { bookingCodes, isServicePayment });
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

        // GET: /Booking/CheckStatus?bookingCodes=BK-001,BK-002&isServicePayment=true
        [HttpGet]
        public async Task<IActionResult> CheckStatus(string bookingCodes, bool isServicePayment = false)
        {
            if (isServicePayment)
            {
                // Do not auto-redirect for pre-existing paid court bookings when paying for additional services
                return Json(new { success = true, allPaid = false });
            }

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

        // POST: /Booking/PayWithWallet
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithWallet(string bookingCodes, bool isServicePayment = false)
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }

            if (string.IsNullOrEmpty(bookingCodes))
            {
                return Json(new { success = false, message = "Không tìm thấy mã đặt sân." });
            }

            if (isServicePayment)
            {
                // Lấy thông tin thanh toán dịch vụ bổ sung từ TempData
                if (TempData.TryGetValue("ServicePaymentInfo", out var serviceInfoJson))
                {
                    TempData.Keep("ServicePaymentInfo");
                    var json = serviceInfoJson as string;
                    if (!string.IsNullOrEmpty(json))
                    {
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        var addAmount = root.GetProperty("AdditionalAmount").GetDecimal();
                        
                        var (success, msg) = await _apiService.PayServicesWithWalletAsync(bookingCodes, addAmount, token);
                        if (success)
                        {
                            TempData["SuccessMessage"] = "Thanh toán dịch vụ bổ sung từ ví thành công!";
                            return Json(new { success = true, redirectUrl = Url.Action("Index", "MyBookings") });
                        }
                        return Json(new { success = false, message = msg });
                    }
                }
                return Json(new { success = false, message = "Không tìm thấy thông tin dịch vụ mua thêm." });
            }
            else
            {
                var (success, msg) = await _apiService.PayBookingWithWalletAsync(bookingCodes, token);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Đặt sân thành công! Tổng cộng đã được thanh toán từ ví.";
                    return Json(new { success = true, redirectUrl = Url.Action("Success", new { bookingCodes }) });
                }
                return Json(new { success = false, message = msg });
            }
        }

        private string? GetToken()
        {
            var token = HttpContext.Session.GetString(Services.Api.JwtForwardingHandler.SessionTokenKey);
            if (!string.IsNullOrWhiteSpace(token))
                return token;

            try
            {
                token = Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext, "access_token").GetAwaiter().GetResult();
            }
            catch { }

            if (string.IsNullOrWhiteSpace(token))
            {
                token = User.FindFirst(Services.Api.JwtForwardingHandler.AccessTokenClaimType)?.Value;
            }

            if (!string.IsNullOrWhiteSpace(token) && HttpContext.Session.IsAvailable)
            {
                HttpContext.Session.SetString(Services.Api.JwtForwardingHandler.SessionTokenKey, token);
            }

            return token;
        }
    }
}
