using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ReportsController(IReportService reportService) : Controller
{
    public async Task<IActionResult> Revenue(string period = "Month")
    {
        var data = await reportService.GetRevenueReportAsync(period);
        var vm = new RevenueReportViewModel
        {
            Period = period,
            DataPoints = data,
            TotalRevenue = data.Sum(d => d.Revenue),
            TotalBookings = data.Sum(d => d.Bookings)
        };
        return View(vm);
    }

    public async Task<IActionResult> CourtUsage()
    {
        var vm = new CourtUsageReportViewModel
        {
            UsageData = await reportService.GetCourtUsageAsync()
        };
        return View(vm);
    }
}
