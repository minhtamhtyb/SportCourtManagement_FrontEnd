using Microsoft.AspNetCore.Mvc;

namespace SportCourtManagement_FrontEnd.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (role is "Admin" or "Staff" or "Coach")
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        return RedirectToAction("Login", "Account");
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
