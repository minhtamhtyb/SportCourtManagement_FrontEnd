using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Tournaments;
using SportCourtManagement_FrontEnd.Services;
using SportCourtManagement_FrontEnd.Services.Api;

namespace SportCourtManagement_FrontEnd.Controllers;

[Authorize]
public class TournamentsController : Controller
{
  private readonly ICourtApiService _apiService;

  public TournamentsController(ICourtApiService apiService)
  {
    _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
  }


  // GET: /Tournaments/AdminIndex (Màn hình 3)
  [HttpGet]
  public async Task<IActionResult> AdminIndex(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int page = 1)
  {
    var pagedData = await _apiService.GetPagedAdminTournamentsAsync(keyword, fromDate, toDate, status, page, 10);
    var vm = new TournamentListViewModel
    {
      PagedData = pagedData,
      Keyword = keyword,
      FromDate = fromDate,
      ToDate = toDate,
      Status = status
    };
    return View(vm);
  }

  // POST: /Tournaments/UpdateStatus (Màn hình 3)
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> UpdateStatus(int id, string status, string? cancelReason, string returnUrl)
  {
    var success = await _apiService.UpdateTournamentStatusAsync(id, status, cancelReason);
    if (success) TempData["SuccessMessage"] = $"Cập nhật trạng thái giải đấu #{id} thành công!";
    else TempData["ErrorMessage"] = $"Lỗi khi cập nhật trạng thái giải đấu #{id}.";

    if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);
    return RedirectToAction(nameof(AdminIndex));
  }

  // GET: /Tournaments/AdminDetail/5 (Màn hình chi tiết giải đấu cho Admin)
  [HttpGet]
  public async Task<IActionResult> AdminDetail(int id)
  {
    var tour = await _apiService.GetMyTournamentDetailAsync(id);
    if (tour == null)
    {
      TempData["ErrorMessage"] = "Không tìm thấy thông tin giải đấu hoặc bạn không có quyền truy cập.";
      return RedirectToAction(nameof(AdminIndex));
    }
    return View(tour);
  }

  // GET: /Tournaments/PublicIndex (Màn hình 4)
  [HttpGet]
  [AllowAnonymous]
  public async Task<IActionResult> PublicIndex(string? keyword, int page = 1)
  {
    var pagedData = await _apiService.GetPagedPublicTournamentsAsync(keyword, page, 12);
    var vm = new PublicTournamentListViewModel
    {
      PagedData = pagedData,
      Keyword = keyword
    };
    return View(vm);
  }

  // GET: /Tournaments/PublicDetail/5 (Màn hình 4)
  [HttpGet]
  [AllowAnonymous]
  public async Task<IActionResult> PublicDetail(int id)
  {
    var pagedData = await _apiService.GetPagedPublicTournamentsAsync(null, 1, 100);
    var item = pagedData.Items.Find(t => t.TournamentId == id);
    if (item == null)
    {
      TempData["ErrorMessage"] = "Không tìm thấy thông tin giải đấu công khai.";
      return RedirectToAction(nameof(PublicIndex));
    }
    return View(item);
  }

  // GET: /Tournaments/MyTournaments (Màn hình 5)
  [HttpGet]
  public async Task<IActionResult> MyTournaments(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int page = 1)
  {
    var pagedData = await _apiService.GetPagedMyTournamentsAsync(keyword, fromDate, toDate, status, page, 10);
    var vm = new TournamentListViewModel
    {
      PagedData = pagedData,
      Keyword = keyword,
      FromDate = fromDate,
      ToDate = toDate,
      Status = status
    };
    return View(vm);
  }

  // GET: /Tournaments/Create (Màn hình 5)
  [HttpGet]
  public async Task<IActionResult> Create()
  {
    var courtsSearch = await _apiService.SearchCourtsAsync(new Models.Courts.CourtSearchParams { PageSize = 50 });
    ViewBag.Courts = courtsSearch.Items;
    ViewBag.CourtTypes = await _apiService.GetCourtTypesAsync();
    ViewBag.TimeSlots = await _apiService.GetTimeSlotsAsync();
    
    var complexServices = new List<SportCourtManagement_FrontEnd.Models.DTOs.ComplexCourtTypeServiceDto>();
    if (courtsSearch.Items != null)
    {
      var complexIds = new List<int> { 1 };
      foreach (var cid in complexIds)
      {
        var svcs = await _apiService.GetComplexServicesAsync(cid);
        complexServices.AddRange(svcs);
      }
    }
    ViewBag.ComplexServices = complexServices;
    
    return View(new CreateTournamentFormDto());
  }

  // POST: /Tournaments/Create (Màn hình 5)
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create(CreateTournamentFormDto form)
  {
    if (!ModelState.IsValid)
    {
      var courtsSearch = await _apiService.SearchCourtsAsync(new Models.Courts.CourtSearchParams { PageSize = 50 });
      ViewBag.Courts = courtsSearch.Items;
      ViewBag.CourtTypes = await _apiService.GetCourtTypesAsync();
      ViewBag.TimeSlots = await _apiService.GetTimeSlotsAsync();
      
      var complexServices = new List<SportCourtManagement_FrontEnd.Models.DTOs.ComplexCourtTypeServiceDto>();
      if (courtsSearch.Items != null)
      {
        var complexIds = new List<int> { 1 };
        foreach (var cid in complexIds)
        {
          var svcs = await _apiService.GetComplexServicesAsync(cid);
          complexServices.AddRange(svcs);
        }
      }
      ViewBag.ComplexServices = complexServices;
      
      return View(form);
    }

    if (form.CourtSelections != null)
    {
      form.CourtSelections = form.CourtSelections.Where(c => c.SlotIds != null && c.SlotIds.Count > 0).ToList();
      foreach (var sel in form.CourtSelections)
      {
         if (sel.Services != null)
         {
             sel.Services = sel.Services.Where(s => s.Quantity > 0).ToList();
         }
      }
    }

    var (created, errMsg) = await _apiService.CreateTournamentResultAsync(form);
    if (created == null)
    {
      ModelState.AddModelError("", !string.IsNullOrWhiteSpace(errMsg) ? errMsg : "Đặt giải đấu thất bại. Khung giờ chọn có thể đã kín lịch.");
      var courtsSearch = await _apiService.SearchCourtsAsync(new Models.Courts.CourtSearchParams { PageSize = 50 });
      ViewBag.Courts = courtsSearch.Items;
      ViewBag.CourtTypes = await _apiService.GetCourtTypesAsync();
      ViewBag.TimeSlots = await _apiService.GetTimeSlotsAsync();
      
      var complexServices = new List<SportCourtManagement_FrontEnd.Models.DTOs.ComplexCourtTypeServiceDto>();
      if (courtsSearch.Items != null)
      {
        var complexIds = new List<int> { 1 };
        foreach (var cid in complexIds)
        {
          var svcs = await _apiService.GetComplexServicesAsync(cid);
          complexServices.AddRange(svcs);
        }
      }
      ViewBag.ComplexServices = complexServices;
      
      return View(form);
    }

    TempData["SuccessMessage"] = "Đăng ký tổ chức giải đấu thành công! Vui lòng tiến hành thanh toán.";
    return RedirectToAction("Payment", new { id = created.TournamentId });
  }

  /// <summary>Creates tournament via AJAX JSON payload without reloading page.</summary>
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> CreateJson([FromBody] CreateTournamentFormDto form)
  {
    if (!ModelState.IsValid)
    {
      return BadRequest(new { success = false, message = "Dữ liệu gửi lên không hợp lệ. Vui lòng kiểm tra lại thông tin giải đấu." });
    }

    if (form.CourtSelections != null)
    {
      form.CourtSelections = form.CourtSelections.Where(c => c.SlotIds != null && c.SlotIds.Count > 0).ToList();
      foreach (var sel in form.CourtSelections)
      {
         if (sel.Services != null)
         {
             sel.Services = sel.Services.Where(s => s.Quantity > 0).ToList();
         }
      }
    }

    if (form.CourtSelections == null || !form.CourtSelections.Any())
    {
      return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất 1 sân và 1 khung giờ thi đấu!" });
    }

    HttpContext.Session.SetString("DraftTournamentForm", System.Text.Json.JsonSerializer.Serialize(form));
    return Ok(new { success = true, redirectUrl = Url.Action("Payment", "Tournaments", new { id = 0 }) });
  }

  // GET: /Tournaments/Payment/5
  [HttpGet]
  public async Task<IActionResult> Payment(int id = 0)
  {
    var token = GetToken();
    var wallet = await _apiService.GetWalletBalanceAsync(token);
    ViewBag.Wallet = wallet;

    if (id > 0)
    {
      var tour = await _apiService.GetMyTournamentDetailAsync(id);
      if (tour == null)
      {
        TempData["ErrorMessage"] = "Không tìm thấy giải đấu hoặc bạn không có quyền truy cập.";
        return RedirectToAction(nameof(MyTournaments));
      }

      if (string.Equals(tour.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(tour.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
      {
        return RedirectToAction(nameof(PaymentSuccess), new { id = tour.TournamentId });
      }

      if (string.Equals(tour.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
      {
        TempData["ErrorMessage"] = $"Giải đấu #{tour.TournamentId} đã bị hủy, không thể tiếp tục thanh toán.";
        return RedirectToAction(nameof(MyTournaments));
      }

      var qrCode = await _apiService.GetSePayQrCodeAsync($"TM-{id}");
      ViewBag.QrCode = qrCode;

      return View(tour);
    }

    // Draft tournament payment screen
    var draftJson = HttpContext.Session.GetString("DraftTournamentForm");
    if (string.IsNullOrEmpty(draftJson))
    {
      TempData["ErrorMessage"] = "Không tìm thấy thông tin đăng ký giải đấu.";
      return RedirectToAction(nameof(Create));
    }

    var form = System.Text.Json.JsonSerializer.Deserialize<CreateTournamentFormDto>(draftJson);
    if (form == null)
    {
      TempData["ErrorMessage"] = "Thông tin đăng ký giải đấu không hợp lệ.";
      return RedirectToAction(nameof(Create));
    }

    var courtsSearch = await _apiService.SearchCourtsAsync(new Models.Courts.CourtSearchParams { PageSize = 100 });
    var courtsList = courtsSearch.Items ?? new();
    var timeSlotsList = await _apiService.GetTimeSlotsAsync() ?? new();

    var complexServices = new List<SportCourtManagement_FrontEnd.Models.DTOs.ComplexCourtTypeServiceDto>();
    var complexIds = new List<int> { 1 };
    foreach (var cid in complexIds)
    {
      var svcs = await _apiService.GetComplexServicesAsync(cid);
      complexServices.AddRange(svcs);
    }

    var draftTour = new TournamentDto
    {
      TournamentId = 0,
      TournamentName = form.TournamentName,
      Description = form.Description,
      Status = "Draft",
      CreatedAt = DateTime.UtcNow,
      Bookings = new List<TournamentBookingItemDto>()
    };

    if (form.CourtSelections != null)
    {
      foreach (var sel in form.CourtSelections)
      {
        var court = courtsList.FirstOrDefault(c => c.CourtId == sel.CourtId);
        Models.Courts.CourtDetailDto? courtDetail = null;
        try { courtDetail = await _apiService.GetCourtDetailAsync(sel.CourtId); } catch { }

        // Calculate services ONCE per court selection (not per slot)
        var bookingServices = new List<TournamentBookingServiceItemDto>();
        decimal serviceTotal = 0;
        if (sel.Services != null)
        {
          foreach (var s in sel.Services)
          {
            if (s.Quantity > 0)
            {
              var matchSvc = complexServices.FirstOrDefault(cs => cs.ServiceId == s.ServiceId);
              decimal unitPrice = matchSvc?.Price ?? 0;
              decimal itemTotal = unitPrice * s.Quantity;
              serviceTotal += itemTotal;

              bookingServices.Add(new TournamentBookingServiceItemDto
              {
                ServiceId = s.ServiceId,
                ServiceName = matchSvc?.ServiceName ?? $"Dịch vụ #{s.ServiceId}",
                Quantity = s.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = itemTotal
              });
            }
          }
        }

        if (sel.SlotIds != null)
        {
          bool isFirstSlot = true;
          foreach (var slotId in sel.SlotIds)
          {
            var slotItem = timeSlotsList.FirstOrDefault(s => s.SlotId == slotId);
            
            decimal courtPrice = 0;
            var pricing = courtDetail?.Pricings?.FirstOrDefault(p => p.SlotId == slotId);
            if (pricing != null && pricing.Price > 0)
            {
              courtPrice = pricing.Price;
            }
            else
            {
              decimal hours = 1.5m;
              if (slotItem != null && TimeSpan.TryParse(slotItem.StartTime, out var st) && TimeSpan.TryParse(slotItem.EndTime, out var et))
              {
                hours = (decimal)(et - st).TotalHours;
              }
              courtPrice = (courtDetail?.PricePerHour > 0 ? courtDetail.PricePerHour : (court?.PricePerHour > 0 ? court.PricePerHour : 100000m)) * (hours > 0 ? hours : 1);
            }

            // Only attach services to the first slot's booking to avoid duplication
            draftTour.Bookings.Add(new TournamentBookingItemDto
            {
              CourtId = sel.CourtId,
              CourtName = court?.CourtName ?? "Sân thể thao",
              SlotName = slotItem?.SlotName ?? $"Slot #{slotId}",
              StartTime = slotItem?.StartTime ?? "",
              EndTime = slotItem?.EndTime ?? "",
              BookingDate = sel.BookingDate,
              TotalAmount = courtPrice + (isFirstSlot ? serviceTotal : 0),
              Status = "Pending",
              Services = isFirstSlot ? bookingServices : new List<TournamentBookingServiceItemDto>()
            });
            isFirstSlot = false;
          }
        }
      }
    }

    draftTour.TotalAmount = draftTour.Bookings.Sum(b => b.TotalAmount);
    ViewBag.DraftForm = form;
    return View(draftTour);
  }

  // GET: /Tournaments/CheckPaymentStatus/5
  [HttpGet]
  public async Task<IActionResult> CheckPaymentStatus(int id)
  {
    if (id <= 0)
    {
      return Ok(new { success = true, status = "Draft", isPaid = false, isCancelled = false, isExpired = false });
    }

    var tour = await _apiService.GetMyTournamentDetailAsync(id);
    if (tour == null)
    {
      return NotFound(new { success = false, message = "Không tìm thấy thông tin giải đấu." });
    }

    bool isPaid = string.Equals(tour.Status, "Paid", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(tour.Status, "Confirmed", StringComparison.OrdinalIgnoreCase);
    bool isCancelled = string.Equals(tour.Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
    bool isExpired = tour.ExpiredAt.HasValue && DateTime.SpecifyKind(tour.ExpiredAt.Value, DateTimeKind.Utc) < DateTime.UtcNow && string.Equals(tour.Status, "Pending", StringComparison.OrdinalIgnoreCase);

    return Ok(new
    {
      success = true,
      status = tour.Status,
      isPaid = isPaid,
      isCancelled = isCancelled,
      isExpired = isExpired,
      redirectUrl = isPaid ? Url.Action(nameof(PaymentSuccess), "Tournaments", new { id = tour.TournamentId }) : Url.Action(nameof(MyDetail), "Tournaments", new { id = tour.TournamentId })
    });
  }

  // GET: /Tournaments/PaymentSuccess/5
  [HttpGet]
  public async Task<IActionResult> PaymentSuccess(int id)
  {
    var tour = await _apiService.GetMyTournamentDetailAsync(id);
    if (tour == null)
    {
      TempData["ErrorMessage"] = "Không tìm thấy thông tin giải đấu.";
      return RedirectToAction(nameof(MyTournaments));
    }

    return View(tour);
  }

  // GET: /Tournaments/MyDetail/5 (Màn hình chi tiết giải đấu của tôi)
  [HttpGet]
  public async Task<IActionResult> MyDetail(int id)
  {
    var tour = await _apiService.GetMyTournamentDetailAsync(id);
    if (tour == null)
    {
      TempData["ErrorMessage"] = "Không tìm thấy thông tin giải đấu hoặc bạn không có quyền truy cập.";
      return RedirectToAction(nameof(MyTournaments));
    }
    return View(tour);
  }

  // POST: /Tournaments/PayWithWallet
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> PayWithWallet(int tournamentId)
  {
    var token = GetToken();
    if (string.IsNullOrEmpty(token))
      return Json(new { success = false, message = "Bạn cần đăng nhập." });

    if (tournamentId > 0)
    {
      var (success, msg) = await _apiService.PayTournamentWithWalletAsync(tournamentId, token);
      if (success)
      {
        TempData["SuccessMessage"] = "Thanh toán giải đấu thành công!";
        return Json(new { success = true, redirectUrl = Url.Action(nameof(PaymentSuccess), new { id = tournamentId }) });
      }
      return Json(new { success = false, message = msg });
    }

    var draftJson = HttpContext.Session.GetString("DraftTournamentForm");
    if (!string.IsNullOrEmpty(draftJson))
    {
      try
      {
        var form = System.Text.Json.JsonSerializer.Deserialize<CreateTournamentFormDto>(draftJson);
        if (form != null)
        {
          var (created, errMsg) = await _apiService.CreateAndPayTournamentWithWalletAsync(form);
          if (created != null)
          {
            HttpContext.Session.Remove("DraftTournamentForm");
            TempData["SuccessMessage"] = "Tạo và thanh toán giải đấu từ ví thành công!";
            return Json(new { success = true, redirectUrl = Url.Action(nameof(PaymentSuccess), new { id = created.TournamentId }) });
          }
          return Json(new { success = false, message = !string.IsNullOrWhiteSpace(errMsg) ? errMsg : "Thanh toán giải đấu thất bại." });
        }
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = ex.Message });
      }
    }

    return Json(new { success = false, message = "Không tìm thấy thông tin đăng ký giải đấu." });
  }

  private string? GetToken()
  {
    var token = HttpContext.Session.GetString(JwtForwardingHandler.SessionTokenKey);
    if (!string.IsNullOrWhiteSpace(token))
      return token;

    try
    {
      token = Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.GetTokenAsync(HttpContext, "access_token").GetAwaiter().GetResult();
    }
    catch { }

    if (string.IsNullOrWhiteSpace(token))
    {
      token = User.FindFirst(JwtForwardingHandler.AccessTokenClaimType)?.Value;
    }

    if (!string.IsNullOrWhiteSpace(token) && HttpContext.Session.IsAvailable)
    {
      HttpContext.Session.SetString(JwtForwardingHandler.SessionTokenKey, token);
    }

    return token;
  }
}
