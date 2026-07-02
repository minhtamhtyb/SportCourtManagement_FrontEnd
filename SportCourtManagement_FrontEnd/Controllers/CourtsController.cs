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
    public async Task<IActionResult> Index(CourtSearchParams searchParams)
    {
        searchParams.PageSize = 6; // Display 6 courts per page matching Figma design
        var courtsTask = _apiService.SearchCourtsAsync(searchParams);
        var typesTask = _apiService.GetCourtTypesAsync();

        var test = await _apiService.StatusCodeAsync();
        TempData["test"] = test;

        await Task.WhenAll(courtsTask, typesTask);

        var viewModel = new CourtSearchViewModel
        {
            CourtsResult = courtsTask.Result,
            CourtTypes = typesTask.Result,
            SearchParams = searchParams
        };

        return View(viewModel);
    }

    // GET: /Courts/Detail/{id}
    public async Task<IActionResult> Detail(int id, DateOnly? date)
    {

        var court = await _apiService.GetCourtDetailAsync(id);
        if (court == null)
        {
            return NotFound();
        }

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var availabilityTask = _apiService.GetCourtAvailabilityAsync(id, selectedDate);
        var reviewsTask = _apiService.GetCourtReviewsAsync(id, 1, 5); // Fetch first 5 reviews

        // Fetch similar courts (same type, excluding current, take 3)
        var similarParams = new CourtSearchParams
        {
            CourtTypeId = court.CourtType.CourtTypeId,
            PageSize = 4 // Fetch 4 to filter out current one
        };
        var similarTask = _apiService.SearchCourtsAsync(similarParams);

        await Task.WhenAll(availabilityTask, reviewsTask, similarTask);

        var similarCourts = similarTask.Result.Items
            .Where(c => c.CourtId != id)
            .Take(3)
            .ToList();

        var viewModel = new CourtDetailViewModel
        {
            Court = court,
            Availability = availabilityTask.Result ?? new CourtAvailabilityDto { CourtId = id, Date = selectedDate },
            ReviewsResult = reviewsTask.Result,
            SimilarCourts = similarCourts,
            SelectedDate = selectedDate
        };

        return View(viewModel);
    }

    // GET: /Courts/Availability/{id}?date=YYYY-MM-DD (AJAX)
    [HttpGet]
    public async Task<IActionResult> GetAvailability(int id, string date)
    {
        if (!DateOnly.TryParse(date, out var parsedDate))
        {
            parsedDate = DateOnly.FromDateTime(DateTime.Today);
        }

        var availability = await _apiService.GetCourtAvailabilityAsync(id, parsedDate);
        if (availability == null)
        {
            availability = new CourtAvailabilityDto { CourtId = id, Date = parsedDate };
        }

        return PartialView("_AvailabilitySlots", availability);
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

        var success = await _apiService.SubmitReviewAsync(id, bookingId, rating, comment, token);
        if (success)
        {
            TempData["SuccessMessage"] = "Gửi đánh giá thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Gửi đánh giá thất bại. Vui lòng kiểm tra lại thông tin đặt sân.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
}
