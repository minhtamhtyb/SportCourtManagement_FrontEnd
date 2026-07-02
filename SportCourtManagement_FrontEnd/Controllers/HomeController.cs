using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models;
using SportCourtManagement_FrontEnd.Services;
using System.Diagnostics;
using System.Security.Claims;

namespace SportCourtManagement_FrontEnd.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICourtApiService _apiService;

    public HomeController(ILogger<HomeController> logger, ICourtApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role is "Admin" or "Staff" or "Coach")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var featuredCourtsResult = await _apiService.SearchCourtsAsync(new Models.Courts.CourtSearchParams
        {
            SortBy = "rating",
            PageSize = 3
        });
        var activePromotions = await _apiService.GetActivePromotionsAsync();

        var viewModel = new Models.ViewModels.HomePageViewModel
        {
            FeaturedCourts = featuredCourtsResult?.Items ?? new(),
            ActivePromotions = activePromotions ?? new()
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
