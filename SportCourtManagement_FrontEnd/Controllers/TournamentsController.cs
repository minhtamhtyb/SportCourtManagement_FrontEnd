using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Tournaments;
using SportCourtManagement_FrontEnd.Services;

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

    var (created, errMsg) = await _apiService.CreateTournamentResultAsync(form);
    if (created == null)
    {
      return Conflict(new { success = false, message = !string.IsNullOrWhiteSpace(errMsg) ? errMsg : "Đặt giải đấu thất bại. Khung giờ chọn có thể đã kín lịch hoặc đang được giữ chỗ." });
    }

    return Ok(new { success = true, redirectUrl = Url.Action("Payment", "Tournaments", new { id = created.TournamentId }) });
  }

  // GET: /Tournaments/Payment/5
  [HttpGet]
  public async Task<IActionResult> Payment(int id)
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

    if (tour.ExpiredAt.HasValue && DateTime.SpecifyKind(tour.ExpiredAt.Value, DateTimeKind.Utc) < DateTime.UtcNow && string.Equals(tour.Status, "Pending", StringComparison.OrdinalIgnoreCase))
    {
      TempData["ErrorMessage"] = $"Đơn đặt giải đấu #{tour.TournamentId} đã hết hạn giữ chỗ (10 phút). Vui lòng đặt lại giải đấu mới.";
      return RedirectToAction(nameof(MyDetail), new { id = tour.TournamentId });
    }

    var qrCode = await _apiService.GetSePayQrCodeAsync($"TM-{id}");
    ViewBag.QrCode = qrCode;

    return View(tour);
  }

  // GET: /Tournaments/CheckPaymentStatus/5
  [HttpGet]
  public async Task<IActionResult> CheckPaymentStatus(int id)
  {
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
}
