using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Promotions;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers;

public class PromotionsController : Controller
{
  private readonly ICourtApiService _apiService;

  public PromotionsController(ICourtApiService apiService)
  {
    _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
  }

  private string? GetToken() => Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"];

  // GET: /Promotions
  [HttpGet]
  public async Task<IActionResult> Index(string? keyword, bool? isActive, int page = 1)
  {
    var token = GetToken();
    var pagedData = await _apiService.GetPagedPromotionsAsync(keyword, isActive, page, 10, token);

    var vm = new PromotionListViewModel
    {
      PagedData = pagedData,
      Keyword = keyword,
      IsActive = isActive
    };
    return View(vm);
  }

  // GET: /Promotions/Create
  [HttpGet]
  public IActionResult Create()
  {
    return View(new PromotionFormDto());
  }

  // POST: /Promotions/Create
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create(PromotionFormDto form)
  {
    if (!ModelState.IsValid) return View(form);

    var token = GetToken();
    var created = await _apiService.CreatePromotionAsync(form, token);
    if (created == null)
    {
      ModelState.AddModelError("", "Tạo khuyến mãi thất bại hoặc mã đã tồn tại.");
      return View(form);
    }

    TempData["SuccessMessage"] = "Tạo chương trình khuyến mãi thành công!";
    return RedirectToAction(nameof(Index));
  }

  // GET: /Promotions/Edit/5
  [HttpGet]
  public async Task<IActionResult> Edit(int id)
  {
    var token = GetToken();
    // Fetch paged list or call edit helper
    var pagedData = await _apiService.GetPagedPromotionsAsync(null, null, 1, 100, token);
    var item = pagedData.Items.Find(p => p.PromotionId == id);
    if (item == null) return NotFound();

    var form = new PromotionFormDto
    {
      PromotionId = item.PromotionId,
      PromoCode = item.PromoCode,
      PromoName = item.PromoName,
      Description = item.Description,
      DiscountType = item.DiscountType,
      DiscountValue = item.DiscountValue,
      MinOrderAmount = item.MinOrderAmount,
      MaxDiscount = item.MaxDiscount,
      UsageLimit = item.UsageLimit,
      StartDate = item.StartDate,
      EndDate = item.EndDate,
      IsActive = item.IsActive
    };
    return View(form);
  }

  // POST: /Promotions/Edit/5
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(int id, PromotionFormDto form)
  {
    if (!ModelState.IsValid) return View(form);

    var token = GetToken();
    var updated = await _apiService.UpdatePromotionAsync(id, form, token);
    if (updated == null)
    {
      ModelState.AddModelError("", "Cập nhật thất bại.");
      return View(form);
    }

    TempData["SuccessMessage"] = "Cập nhật khuyến mãi thành công!";
    return RedirectToAction(nameof(Index));
  }

  // POST: /Promotions/Delete/5
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Delete(int id)
  {
    var token = GetToken();
    var success = await _apiService.DeletePromotionAsync(id, token);
    if (success) TempData["SuccessMessage"] = "Xóa khuyến mãi thành công!";
    else TempData["ErrorMessage"] = "Không thể xóa khuyến mãi.";
    return RedirectToAction(nameof(Index));
  }
}
