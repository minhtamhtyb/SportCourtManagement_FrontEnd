using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOrStaff")]
public class DashboardController(IReportService reportService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            Summary = await reportService.GetDashboardAsync(),
            CourtStats = await reportService.GetComplexStatsAsync()
        };
        return View(vm);
    }
}
