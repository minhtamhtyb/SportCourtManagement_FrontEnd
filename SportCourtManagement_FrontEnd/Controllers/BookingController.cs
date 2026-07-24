using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Services;
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

            // Load services: Filter by CourtId if selected
            List<ServiceDto> services = new();
            if (courtId.HasValue && courtId.Value > 0)
            {
                services = await _apiService.GetServicesByCourtIdAsync(courtId.Value);
            }
            else
            {
                services = await _apiService.GetServicesAsync();
            }

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

                if (price <= 0 && court != null)
                {
                    var anyPricing = court.Pricings?.FirstOrDefault(p => p.Price > 0);
                    if (anyPricing != null)
                    {
                        price = anyPricing.Price;
                    }
                    else
                    {
                        decimal hours = 1.5m;
                        if (TimeSpan.TryParse(ts.StartTime, out var sTime) && TimeSpan.TryParse(ts.EndTime, out var eTime))
                        {
                            hours = (decimal)(eTime - sTime).TotalHours;
                        }
                        price = (court.PricePerHour > 0 ? court.PricePerHour : 100000m) * (hours > 0 ? hours : 1);
                    }
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

            try
            {
                var court = await _apiService.GetCourtDetailAsync(courtId);
                var timeSlots = await _apiService.GetTimeSlotsAsync();
                var selectedSlots = timeSlots?.Where(ts => targetSlots.Contains(ts.SlotId)).OrderBy(ts => ts.StartTime).ToList() ?? new();

                var minStart = selectedSlots.Count > 0 && TimeSpan.TryParse(selectedSlots.First().StartTime, out var st2) ? st2 : TimeSpan.Zero;
                var maxEnd = selectedSlots.Count > 0 && TimeSpan.TryParse(selectedSlots.Last().EndTime, out var et2) ? et2 : TimeSpan.Zero;

                string slotRangeName = selectedSlots.Count > 1
                    ? $"{CleanSlotName(selectedSlots.First().SlotName)} - {CleanSlotName(selectedSlots.Last().SlotName)}"
                    : (selectedSlots.FirstOrDefault() != null ? CleanSlotName(selectedSlots.First().SlotName) : "Slot");

                var allDates = new List<DateTime>();
                for (var date = parsedStartDate.Date; date <= parsedEndDate.Date; date = date.AddDays(1))
                {
                    if (daysOfWeek.Contains((int)date.DayOfWeek))
                        allDates.Add(date);
                }

                if (allDates.Count == 0)
                {
                    TempData["ErrorMessage"] = "Không có ngày nào phù hợp trong khoảng thời gian đã chọn.";
                    return RedirectToAction("Index", new { courtId });
                }

                decimal daySubTotal = 0;
                foreach (var sId in targetSlots)
                {
                    var pricing = court?.Pricings?.FirstOrDefault(p => p.SlotId == sId && p.Price > 0);
                    if (pricing != null)
                    {
                        daySubTotal += pricing.Price;
                    }
                    else
                    {
                        var anyPricing = court?.Pricings?.FirstOrDefault(p => p.Price > 0);
                        if (anyPricing != null)
                        {
                            daySubTotal += anyPricing.Price;
                        }
                        else
                        {
                            var slotItem = selectedSlots.FirstOrDefault(s => s.SlotId == sId);
                            decimal hours = 1.5m;
                            if (slotItem != null && TimeSpan.TryParse(slotItem.StartTime, out var sT) && TimeSpan.TryParse(slotItem.EndTime, out var eT))
                            {
                                hours = (decimal)(eT - sT).TotalHours;
                            }
                            daySubTotal += (court?.PricePerHour > 0 ? court.PricePerHour : 100000m) * (hours > 0 ? hours : 1);
                        }
                    }
                }

                var allSessionDrafts = new List<SingularBookingResponseDto>();
                for (int i = 0; i < allDates.Count; i++)
                {
                    allSessionDrafts.Add(new SingularBookingResponseDto
                    {
                        BookingCode = $"DRAFT-REC-{i + 1}",
                        CourtId = courtId,
                        CourtName = court?.CourtName ?? "Sân thể thao",
                        SlotId = targetSlots.First(),
                        SlotName = slotRangeName,
                        BookingDate = allDates[i],
                        StartTime = minStart,
                        EndTime = maxEnd,
                        SubTotal = daySubTotal,
                        DiscountAmount = 0,
                        TotalAmount = daySubTotal,
                        Status = "Draft"
                    });
                }

                var request = new RecurringBookingRequestDto
                {
                    CourtId = courtId,
                    SlotId = targetSlots.First(),
                    SlotIds = targetSlots,
                    StartDate = DateOnly.FromDateTime(parsedStartDate),
                    EndDate = DateOnly.FromDateTime(parsedEndDate),
                    DaysOfWeek = daysOfWeek,
                    PromotionCode = promoCode,
                    Note = note
                };

                HttpContext.Session.Remove("DraftSingleRequest");
                HttpContext.Session.SetString("DraftRecurringRequest", JsonSerializer.Serialize(request));
                TempData["BookingResponses"] = JsonSerializer.Serialize(allSessionDrafts);

                return RedirectToAction("Payment", new { bookingCodes = "DRAFT-RECURRING" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi thiết lập đơn định kỳ: " + ex.Message;
                return RedirectToAction("Index", new { courtId });
            }
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

            var request = new CreateBookingRequestDto
            {
                CourtId = courtId,
                SlotId = slotIds.First(),
                SlotIds = slotIds,
                BookingDate = parsedDate,
                PromoCode = promoCode,
                BookingServices = serviceItems
            };

            var court = await _apiService.GetCourtDetailAsync(courtId);
            var timeSlots = await _apiService.GetTimeSlotsAsync();
            var selectedSlots = timeSlots?.Where(ts => slotIds.Contains(ts.SlotId)).OrderBy(ts => ts.StartTime).ToList() ?? new();

            var minStart = selectedSlots.Count > 0 && TimeSpan.TryParse(selectedSlots.First().StartTime, out var st1) ? st1 : TimeSpan.Zero;
            var maxEnd = selectedSlots.Count > 0 && TimeSpan.TryParse(selectedSlots.Last().EndTime, out var et1) ? et1 : TimeSpan.Zero;

            string slotRangeName = selectedSlots.Count > 1
                ? $"{CleanSlotName(selectedSlots.First().SlotName)} - {CleanSlotName(selectedSlots.Last().SlotName)}"
                : (selectedSlots.FirstOrDefault() != null ? CleanSlotName(selectedSlots.First().SlotName) : "Slot");

            decimal subTotal = 0;
            foreach (var sId in slotIds)
            {
                var pricing = court?.Pricings?.FirstOrDefault(p => p.SlotId == sId && p.Price > 0);
                if (pricing != null)
                {
                    subTotal += pricing.Price;
                }
                else
                {
                    var anyPricing = court?.Pricings?.FirstOrDefault(p => p.Price > 0);
                    if (anyPricing != null)
                    {
                        subTotal += anyPricing.Price;
                    }
                    else
                    {
                        var slotItem = selectedSlots.FirstOrDefault(s => s.SlotId == sId);
                        decimal hours = 1.5m;
                        if (slotItem != null && TimeSpan.TryParse(slotItem.StartTime, out var sT) && TimeSpan.TryParse(slotItem.EndTime, out var eT))
                        {
                            hours = (decimal)(eT - sT).TotalHours;
                        }
                        subTotal += (court?.PricePerHour > 0 ? court.PricePerHour : 100000m) * (hours > 0 ? hours : 1);
                    }
                }
            }

            var draftServices = new List<SingularBookingServiceResponseDto>();
            decimal serviceTotalAmount = 0;
            if (serviceItems.Any())
            {
                var availableServices = await _apiService.GetServicesByCourtIdAsync(courtId) ?? new();
                foreach (var item in serviceItems)
                {
                    var matched = availableServices.FirstOrDefault(s => s.ServiceId == item.ServiceId);
                    decimal unitPrice = matched?.Price ?? 0;
                    decimal itemTotal = unitPrice * item.Quantity;
                    serviceTotalAmount += itemTotal;

                    draftServices.Add(new SingularBookingServiceResponseDto
                    {
                        ServiceId = item.ServiceId,
                        ServiceName = matched?.ServiceName ?? $"Dịch vụ #{item.ServiceId}",
                        Quantity = item.Quantity,
                        Price = unitPrice,
                        TotalPrice = itemTotal
                    });
                }
            }

            var draftDto = new SingularBookingResponseDto
            {
                BookingCode = "DRAFT-SINGLE",
                CourtId = courtId,
                CourtName = court?.CourtName ?? "Sân thể thao",
                SlotId = slotIds.First(),
                SlotName = slotRangeName,
                BookingDate = parsedDate,
                StartTime = minStart,
                EndTime = maxEnd,
                SubTotal = subTotal,
                ServicesAmount = serviceTotalAmount,
                DiscountAmount = 0,
                TotalAmount = subTotal + serviceTotalAmount,
                Status = "Draft",
                BookingServices = draftServices
            };

            HttpContext.Session.Remove("DraftRecurringRequest");
            HttpContext.Session.SetString("DraftSingleRequest", JsonSerializer.Serialize(request));
            TempData["BookingResponses"] = JsonSerializer.Serialize(new List<SingularBookingResponseDto> { draftDto });

            return RedirectToAction("Payment", new { bookingCodes = "DRAFT-SINGLE" });
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

            if (isServicePayment)
            {
                string? serviceInfoJson = null;
                if (TempData.TryGetValue("ServicePaymentInfo", out var obj) && obj is string jsonFromTemp)
                {
                    serviceInfoJson = jsonFromTemp;
                    TempData.Keep("ServicePaymentInfo");
                }
                if (string.IsNullOrEmpty(serviceInfoJson) && !string.IsNullOrEmpty(bookingCodes))
                {
                    serviceInfoJson = HttpContext.Session.GetString($"ServicePaymentInfo_{bookingCodes}");
                }

                if (!string.IsNullOrEmpty(serviceInfoJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(serviceInfoJson);
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

            var token = GetToken();

            // Handle Recurring Booking Draft payment
            if (bookingCodes.Contains("DRAFT-REC") || bookingCodes.Contains("DRAFT-RECURRING") || (bookingCodes.StartsWith("DRAFT") && HttpContext.Session.GetString("DraftRecurringRequest") != null && !bookingCodes.Contains("DRAFT-SINGLE")))
            {
                var jsonStr = HttpContext.Session.GetString("DraftRecurringRequest");
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    try
                    {
                        var request = JsonSerializer.Deserialize<RecurringBookingRequestDto>(jsonStr);
                        if (request != null)
                        {
                            var result = await _apiService.CreateRecurringBookingAsync(request, token);
                            HttpContext.Session.Remove("DraftRecurringRequest");
                            
                            string msg = $"Đặt sân định kỳ thành công! Đã tự động thanh toán {result.TotalEstimatedAmount:N0}đ từ ví cho {result.TotalBookedSessions} buổi.";
                            if (result.ConflictDates != null && result.ConflictDates.Count > 0)
                            {
                                msg += $" (Các ngày bị trùng lịch đã được bỏ qua: {string.Join(", ", result.ConflictDates.Distinct())}).";
                            }
                            TempData["SuccessMessage"] = msg;
                            return RedirectToAction("Index", "MyBookings");
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "Thanh toán đặt sân định kỳ thất bại: " + ex.Message;
                        return RedirectToAction("Index");
                    }
                }
            }

            // Handle Single Booking Draft payment
            if (bookingCodes.Contains("DRAFT-SINGLE") || (bookingCodes.StartsWith("DRAFT") && HttpContext.Session.GetString("DraftSingleRequest") != null))
            {
                var jsonStr = HttpContext.Session.GetString("DraftSingleRequest");
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    try
                    {
                        var request = JsonSerializer.Deserialize<CreateBookingRequestDto>(jsonStr);
                        if (request != null)
                        {
                            var result = await _apiService.CreateSingularBookingAsync(request, token);
                            HttpContext.Session.Remove("DraftSingleRequest");
                            TempData["SuccessMessage"] = $"Đặt sân thành công! Đã tự động thanh toán {result.TotalAmount:N0}đ từ ví cho đơn #{result.BookingCode}.";
                            return RedirectToAction("Index", "MyBookings");
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "Thanh toán đơn đặt sân thất bại: " + ex.Message;
                        return RedirectToAction("Index");
                    }
                }
            }

            var codes = bookingCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var successCount = 0;
            var lastMessage = "";

            foreach (var code in codes)
            {
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
                    string? serviceInfoJson = null;
                    if (TempData.TryGetValue("ServicePaymentInfo", out var obj) && obj is string jsonFromTemp)
                    {
                        serviceInfoJson = jsonFromTemp;
                    }
                    if (string.IsNullOrEmpty(serviceInfoJson) && !string.IsNullOrEmpty(bookingCodes))
                    {
                        serviceInfoJson = HttpContext.Session.GetString($"ServicePaymentInfo_{bookingCodes}");
                    }

                    if (!string.IsNullOrEmpty(serviceInfoJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(serviceInfoJson);
                            var root = doc.RootElement;
                            var bookingId = root.GetProperty("BookingId").GetInt32();
                            var addedText = root.GetProperty("AddedServicesText").GetString();
                            var quantitiesJson = root.GetProperty("ServiceQuantities").GetRawText();
                            var quantities = JsonSerializer.Deserialize<Dictionary<int, int>>(quantitiesJson) ?? new();

                            await ApplyServicePaymentOverrideAsync(bookingId, quantities, token);
                            TempData["SuccessMessage"] = $"Xác nhận thanh toán dịch vụ bổ sung thành công: {addedText}!";
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error completing service payment in SimulatePayment");
                            TempData["SuccessMessage"] = "Đã nhận thanh toán dịch vụ bổ sung!";
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Xác nhận thanh toán dịch vụ bổ sung thành công!";
                    }
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

            // Handle Recurring Booking Draft payment
            if (bookingCodes.Contains("DRAFT-REC") || bookingCodes.Contains("DRAFT-RECURRING") || (bookingCodes.StartsWith("DRAFT") && HttpContext.Session.GetString("DraftRecurringRequest") != null && !bookingCodes.Contains("DRAFT-SINGLE")))
            {
                var jsonStr = HttpContext.Session.GetString("DraftRecurringRequest");
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    try
                    {
                        var request = JsonSerializer.Deserialize<RecurringBookingRequestDto>(jsonStr);
                        if (request != null)
                        {
                            var result = await _apiService.CreateRecurringBookingAsync(request, token);
                            HttpContext.Session.Remove("DraftRecurringRequest");

                            string msg = $"Đặt sân định kỳ thành công! Đã tự động thanh toán {result.TotalEstimatedAmount:N0}đ từ ví cho {result.TotalBookedSessions} buổi.";
                            if (result.ConflictDates != null && result.ConflictDates.Count > 0)
                            {
                                msg += $" (Các ngày bị trùng lịch đã được bỏ qua: {string.Join(", ", result.ConflictDates.Distinct())}).";
                            }
                            TempData["SuccessMessage"] = msg;
                            return Json(new { success = true, redirectUrl = Url.Action("Index", "MyBookings") });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }

            // Handle Single Booking Draft payment
            if (bookingCodes.Contains("DRAFT-SINGLE") || (bookingCodes.StartsWith("DRAFT") && HttpContext.Session.GetString("DraftSingleRequest") != null))
            {
                var jsonStr = HttpContext.Session.GetString("DraftSingleRequest");
                if (!string.IsNullOrEmpty(jsonStr))
                {
                    try
                    {
                        var request = JsonSerializer.Deserialize<CreateBookingRequestDto>(jsonStr);
                        if (request != null)
                        {
                            var result = await _apiService.CreateSingularBookingAsync(request, token);
                            HttpContext.Session.Remove("DraftSingleRequest");
                            TempData["SuccessMessage"] = $"Đặt sân thành công! Đã thanh toán {result.TotalAmount:N0}đ từ ví cho đơn #{result.BookingCode}.";
                            return Json(new { success = true, redirectUrl = Url.Action("Index", "MyBookings") });
                        }
                    }
                    catch (Exception ex)
                    {
                        return Json(new { success = false, message = ex.Message });
                    }
                }
            }

            if (isServicePayment)
            {
                string? serviceInfoJson = null;
                if (TempData.TryGetValue("ServicePaymentInfo", out var obj) && obj is string jsonFromTemp)
                {
                    serviceInfoJson = jsonFromTemp;
                    TempData.Keep("ServicePaymentInfo");
                }
                if (string.IsNullOrEmpty(serviceInfoJson) && !string.IsNullOrEmpty(bookingCodes))
                {
                    serviceInfoJson = HttpContext.Session.GetString($"ServicePaymentInfo_{bookingCodes}");
                }

                if (!string.IsNullOrEmpty(serviceInfoJson))
                {
                    using var doc = JsonDocument.Parse(serviceInfoJson);
                    var root = doc.RootElement;
                    var bookingId = root.GetProperty("BookingId").GetInt32();
                    var addAmount = root.GetProperty("AdditionalAmount").GetDecimal();
                    var addedText = root.GetProperty("AddedServicesText").GetString();
                    var quantitiesJson = root.GetProperty("ServiceQuantities").GetRawText();
                    var quantities = JsonSerializer.Deserialize<Dictionary<int, int>>(quantitiesJson) ?? new();

                    var (success, msg) = await _apiService.PayServicesWithWalletAsync(bookingCodes, addAmount, token);
                    if (success)
                    {
                        await ApplyServicePaymentOverrideAsync(bookingId, quantities, token);
                        TempData["SuccessMessage"] = $"Thanh toán dịch vụ bổ sung từ ví thành công: {addedText}!";
                        return Json(new { success = true, redirectUrl = Url.Action("Index", "MyBookings") });
                    }
                    return Json(new { success = false, message = msg });
                }
                return Json(new { success = false, message = "Không tìm thấy thông tin dịch vụ mua thêm." });
            }
            else
            {
                var (success, msg) = await _apiService.PayBookingWithWalletAsync(bookingCodes, token);
                if (success)
                {
                    TempData["SuccessMessage"] = $"Đặt sân thành công! Đã thanh toán từ ví điện tử.";
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "MyBookings") });
                }
                return Json(new { success = false, message = msg });
            }
        }

        private async Task ApplyServicePaymentOverrideAsync(int bookingId, Dictionary<int, int> serviceQuantities, string token)
        {
            try
            {
                var (apiSuccess, _) = await _apiService.AddServicesToBookingAsync(bookingId, serviceQuantities, token);
                if (apiSuccess)
                {
                    var session = HttpContext.Session;
                    var json = session.GetString("BookingOverrides");
                    if (!string.IsNullOrEmpty(json))
                    {
                        var overrides = JsonSerializer.Deserialize<List<BookingDetailDto>>(json) ?? new List<BookingDetailDto>();
                        overrides.RemoveAll(o => o.BookingId == bookingId);
                        session.SetString("BookingOverrides", JsonSerializer.Serialize(overrides));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApplyServicePaymentOverrideAsync");
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

        private static string CleanSlotName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name ?? "";
            var idx = name.IndexOf('(');
            return idx > 0 ? name.Substring(0, idx).Trim() : name.Trim();
        }
    }
}
