using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.ViewModels;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers;

public class CourtsController : Controller
{
    private readonly ICourtApiService _apiService;

    public CourtsController(ICourtApiService apiService)
    {
        _apiService = apiService;
    }

    // GET: /Courts
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Courts/Detail/{id}
    public IActionResult Detail(int id, DateOnly? date)
    {
        ViewBag.CourtId = id;
        ViewBag.SelectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        return View();
    }

    // GET: /Courts/GetCourtsJson
    [HttpGet]
    public async Task<IActionResult> GetCourtsJson(CourtSearchParams searchParams)
    {
        var result = await _apiService.SearchCourtsAsync(searchParams);
        return Json(result);
    }

    // GET: /Courts/GetCourtDetailJson/{id}
    [HttpGet]
    public async Task<IActionResult> GetCourtDetailJson(int id)
    {
        var court = await _apiService.GetCourtDetailAsync(id);
        return Json(court);
    }

    // GET: /Courts/GetCourtAvailabilityJson/{id}?date=YYYY-MM-DD
    [HttpGet]
    public async Task<IActionResult> GetCourtAvailabilityJson(int id, string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            parsedDate = DateTime.Today;
        }
        var availability = await _apiService.GetCourtAvailabilityAsync(id, parsedDate);
        return Json(availability);
    }

    // GET: /Courts/GetCourtReviewsJson/{courtId}?pageNumber=N
    [HttpGet]
    public async Task<IActionResult> GetCourtReviewsJson(int courtId, int pageNumber = 1)
    {
        var reviews = await _apiService.GetCourtReviewsAsync(courtId, pageNumber, 5);
        return Json(reviews);
    }

    // GET: /Courts/GetCourtTypesJson
    [HttpGet]
    public async Task<IActionResult> GetCourtTypesJson()
    {
        var types = await _apiService.GetCourtTypesAsync();
        return Json(types);
    }

    // GET: /Courts/Availability/{id}?date=YYYY-MM-DD (AJAX)
    [HttpGet]
    public async Task<IActionResult> GetAvailability([FromQuery] int id, string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            parsedDate = DateTime.Today;
        }

        var availability = await _apiService.GetCourtAvailabilityAsync(id, parsedDate);
        if (availability == null)
        {
            availability = new CourtAvailabilityDto { CourtId = id, Date = parsedDate };
        }

        return PartialView("_AvailabilitySlots", availability);
    }

    // GET: /Courts/CheckAvailabilityJson (AJAX JSON)
    [HttpGet]
    public async Task<IActionResult> CheckAvailabilityJson([FromQuery] int id, string date)
    {
        if (!DateTime.TryParse(date, out var parsedDate))
        {
            return BadRequest(new { message = "Ngày không hợp lệ." });
        }

        var availability = await _apiService.GetCourtAvailabilityAsync(id, parsedDate);
        return Json(availability);
    }

    // GET: /Courts/Reviews/{courtId}?pageNumber=N (AJAX)
    [HttpGet]
    public async Task<IActionResult> GetReviews(int courtId, int pageNumber = 1)
    {
        var reviews = await _apiService.GetCourtReviewsAsync(courtId, pageNumber, 5);
        return PartialView("_ReviewList", reviews);
    }

    // POST: /Courts/SubmitReview/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReview(int id, int bookingId, byte rating, string? comment)
    {
        // Extract token from request cookie or header
        var token = Request.Cookies["jwt"] ?? Request.Cookies["AccessToken"] ?? Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "Bạn cần đăng nhập để gửi đánh giá.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var (success, message) = await _apiService.SubmitReviewAsync(id, bookingId, rating, comment, token);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
}
