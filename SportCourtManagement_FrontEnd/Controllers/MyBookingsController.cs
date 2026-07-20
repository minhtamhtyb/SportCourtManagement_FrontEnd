using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.ViewModels;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers
{
    public class MyBookingsController : Controller
    {
        private readonly ICourtApiService _apiService;

        public MyBookingsController(ICourtApiService apiService)
        {
            _apiService = apiService;
        }

        // GET: /MyBookings
        [HttpGet]
        public async Task<IActionResult> Index(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int page = 1)
        {
            var token = GetToken();
            var pagedData = await _apiService.GetPagedMyBookingsAsync(keyword, fromDate, toDate, status, page, 6, token);
            
            var vm = new MyBookingsPageViewModel
            {
                PagedData = pagedData,
                Keyword = keyword,
                FromDate = fromDate,
                ToDate = toDate,
                Status = status
            };
            return View(vm);
        }

        // GET: /MyBookings/GetAlternativeCourts?bookingId=123
        [HttpGet]
        public async Task<IActionResult> GetAlternativeCourts(int bookingId)
        {
            var token = GetToken();
            var bookings = await _apiService.GetPagedMyBookingsAsync(null, null, null, null, 1, 100, token);
            var booking = bookings?.Items?.FirstOrDefault(b => b.BookingId == bookingId);
            
            var overrides = GetBookingOverrides();
            var overridenBooking = overrides.FirstOrDefault(b => b.BookingId == bookingId);
            if (overridenBooking != null)
            {
                booking = overridenBooking;
            }

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin đặt sân." });
            }

            var courtDetail = await _apiService.GetCourtDetailAsync(booking.CourtId);
            if (courtDetail == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin sân đấu." });
            }

            var searchResult = await _apiService.SearchCourtsAsync(new CourtSearchParams
            {
                PageNumber = 1,
                PageSize = 50,
                Status = "Active"
            });

            var sameTypeCourts = searchResult?.Items?.Where(c => c.CourtTypeId == courtDetail.CourtType?.CourtTypeId && c.CourtId != booking.CourtId).ToList() ?? new();
            var availableAlternativeCourts = new List<object>();

            foreach (var court in sameTypeCourts)
            {
                var availability = await _apiService.GetCourtAvailabilityAsync(court.CourtId, booking.BookingDate);
                if (availability != null)
                {
                    var slot = availability.Slots?.FirstOrDefault(s => s.SlotId == booking.SlotId);
                    if (slot != null && slot.Status == "Available")
                    {
                        availableAlternativeCourts.Add(new
                        {
                            courtId = court.CourtId,
                            courtName = court.CourtName,
                            price = slot.Price,
                            location = court.Location,
                            imageUrl = court.ImageUrl
                        });
                    }
                }
            }

            return Json(new { success = true, courts = availableAlternativeCourts });
        }

        // POST: /MyBookings/ChangeCourt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeCourt(int bookingId, int newCourtId, string newCourtName)
        {
            var token = GetToken();
            var bookings = await _apiService.GetPagedMyBookingsAsync(null, null, null, null, 1, 100, token);
            var booking = bookings?.Items?.FirstOrDefault(b => b.BookingId == bookingId);

            var overrides = GetBookingOverrides();
            var overridenBooking = overrides.FirstOrDefault(b => b.BookingId == bookingId);
            if (overridenBooking != null)
            {
                booking = overridenBooking;
            }

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt sân.";
                return RedirectToAction("Index");
            }

            if (overridenBooking == null)
            {
                overridenBooking = new BookingDetailDto
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    UserId = booking.UserId,
                    CustomerName = booking.CustomerName,
                    CustomerPhone = booking.CustomerPhone,
                    CourtId = booking.CourtId,
                    CourtName = booking.CourtName,
                    SlotId = booking.SlotId,
                    SlotName = booking.SlotName,
                    BookingDate = booking.BookingDate,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    SubTotal = booking.SubTotal,
                    DiscountAmount = booking.DiscountAmount,
                    TotalAmount = booking.TotalAmount,
                    Status = booking.Status,
                    PromotionId = booking.PromotionId,
                    PromotionCode = booking.PromotionCode,
                    Note = booking.Note,
                    CancelReason = booking.CancelReason,
                    CreatedAt = booking.CreatedAt
                };
                overrides.Add(overridenBooking);
            }

            overridenBooking.CourtId = newCourtId;
            overridenBooking.CourtName = newCourtName;

            SaveBookingOverrides(overrides);
            TempData["SuccessMessage"] = $"Đổi sang sân '{newCourtName}' thành công!";

            return RedirectToAction("Index");
        }

        // GET: /MyBookings/GetServices?courtId=1
        [HttpGet]
        public async Task<IActionResult> GetServices(int? courtId)
        {
            var services = courtId.HasValue && courtId.Value > 0
                ? await _apiService.GetServicesByCourtIdAsync(courtId.Value)
                : await _apiService.GetServicesAsync();
            return Json(services);
        }

        // POST: /MyBookings/AddServices
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddServices(int bookingId, Dictionary<int, int> serviceQuantities)
        {
            var token = GetToken();
            var bookings = await _apiService.GetPagedMyBookingsAsync(null, null, null, null, 1, 100, token);
            var booking = bookings?.Items?.FirstOrDefault(b => b.BookingId == bookingId);

            var overrides = GetBookingOverrides();
            var overridenBooking = overrides.FirstOrDefault(b => b.BookingId == bookingId);
            if (overridenBooking != null)
            {
                booking = overridenBooking;
            }

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đặt sân.";
                return RedirectToAction("Index");
            }

            var services = booking.CourtId > 0
                ? await _apiService.GetServicesByCourtIdAsync(booking.CourtId)
                : await _apiService.GetServicesAsync();
            if (services == null || !services.Any())
            {
                services = await _apiService.GetServicesAsync();
            }
            decimal additionalAmount = 0;

            foreach (var kvp in serviceQuantities)
            {
                if (kvp.Value > 0)
                {
                    var service = services.FirstOrDefault(s => s.ServiceId == kvp.Key);
                    if (service != null)
                    {
                        additionalAmount += service.Price * kvp.Value;
                    }
                }
            }

            if (additionalAmount == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một dịch vụ.";
                return RedirectToAction("Index");
            }

            if (overridenBooking == null)
            {
                overridenBooking = new BookingDetailDto
                {
                    BookingId = booking.BookingId,
                    BookingCode = booking.BookingCode,
                    UserId = booking.UserId,
                    CustomerName = booking.CustomerName,
                    CustomerPhone = booking.CustomerPhone,
                    CourtId = booking.CourtId,
                    CourtName = booking.CourtName,
                    SlotId = booking.SlotId,
                    SlotName = booking.SlotName,
                    BookingDate = booking.BookingDate,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    SubTotal = booking.SubTotal,
                    DiscountAmount = booking.DiscountAmount,
                    TotalAmount = booking.TotalAmount,
                    Status = booking.Status,
                    PromotionId = booking.PromotionId,
                    PromotionCode = booking.PromotionCode,
                    Note = booking.Note,
                    CancelReason = booking.CancelReason,
                    CreatedAt = booking.CreatedAt
                };
                overrides.Add(overridenBooking);
            }

            if (overridenBooking.Services == null) overridenBooking.Services = new List<BookingServiceItemDto>();
            foreach (var q in serviceQuantities.Where(kv => kv.Value > 0))
            {
                var s = services.FirstOrDefault(svc => svc.ServiceId == q.Key);
                if (s != null)
                {
                    var existingSvc = overridenBooking.Services.FirstOrDefault(svc => svc.ServiceId == q.Key);
                    if (existingSvc != null)
                    {
                        existingSvc.Quantity += q.Value;
                        existingSvc.TotalPrice += s.Price * q.Value;
                    }
                    else
                    {
                        overridenBooking.Services.Add(new BookingServiceItemDto
                        {
                            ServiceId = s.ServiceId,
                            ServiceName = s.ServiceName,
                            Quantity = q.Value,
                            UnitPrice = s.Price,
                            TotalPrice = s.Price * q.Value
                        });
                    }
                }
            }

            overridenBooking.TotalAmount += additionalAmount;
            string addedServicesText = string.Join(", ", serviceQuantities.Where(q => q.Value > 0).Select(q => {
                var s = services.FirstOrDefault(svc => svc.ServiceId == q.Key);
                return $"{s?.ServiceName ?? "Dịch vụ"} x{q.Value}";
            }));
            overridenBooking.Note = string.IsNullOrEmpty(overridenBooking.Note) 
                ? $"Đặt thêm: {addedServicesText}" 
                : $"{overridenBooking.Note} | Đặt thêm: {addedServicesText}";

            // Persist to backend database
            var (apiSuccess, apiMessage) = await _apiService.AddServicesToBookingAsync(bookingId, serviceQuantities, token);
            if (!apiSuccess)
            {
                TempData["ErrorMessage"] = $"Không thể lưu dịch vụ: {apiMessage}";
                return RedirectToAction("Index");
            }

            SaveBookingOverrides(overrides);

            TempData["SuccessMessage"] = $"Đã thêm dịch vụ: {addedServicesText}. Vui lòng thanh toán số tiền bổ sung!";

            // Pass service payment info for Payment page
            var addedServicesList = serviceQuantities.Where(q => q.Value > 0).Select(q => {
                var s = services.FirstOrDefault(svc => svc.ServiceId == q.Key);
                return new SingularBookingServiceResponseDto
                {
                    ServiceId = q.Key,
                    ServiceName = s?.ServiceName ?? "Dịch vụ",
                    Quantity = q.Value,
                    Price = s?.Price ?? 0,
                    TotalPrice = (s?.Price ?? 0) * q.Value
                };
            }).ToList();

            TempData["ServicePaymentInfo"] = JsonSerializer.Serialize(new
            {
                BookingCode = booking.BookingCode,
                AdditionalAmount = additionalAmount,
                AddedServicesText = addedServicesText,
                Services = addedServicesList
            });

            return RedirectToAction("Payment", "Booking", new { bookingCodes = booking.BookingCode, isServicePayment = true });
        }

        // POST: /MyBookings/SubmitReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int courtId, int bookingId, byte rating, string? comment)
        {
            var token = GetToken();
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi đánh giá.";
                return RedirectToAction("Index");
            }

            var (success, message) = await _apiService.SubmitReviewAsync(courtId, bookingId, rating, comment, token);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction("Index");
        }

        private List<BookingDetailDto> GetBookingOverrides()
        {
            var json = HttpContext.Session.GetString("BookingOverrides");
            if (string.IsNullOrEmpty(json)) return new List<BookingDetailDto>();
            return JsonSerializer.Deserialize<List<BookingDetailDto>>(json) ?? new List<BookingDetailDto>();
        }

        private void SaveBookingOverrides(List<BookingDetailDto> overrides)
        {
            var json = JsonSerializer.Serialize(overrides);
            HttpContext.Session.SetString("BookingOverrides", json);
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
    }
}
