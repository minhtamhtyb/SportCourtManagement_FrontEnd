using Microsoft.AspNetCore.Mvc;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Route("manager")]
    public class ManagerController : Controller
    {
        [Route("staff/shifts")]
        public IActionResult Shifts()
        {
            return View();
        }

        [Route("staff/attendance")]
        public IActionResult Attendance()
        {
            return View();
        }

        [Route("staff/list")]
        public IActionResult StaffList()
        {
            return View();
        }

        [Route("staff/salary")]
        public IActionResult SalaryConfig()
        {
            return View();
        }

        [Route("dashboard")]
        public IActionResult Dashboard()
        {
            return RedirectToAction("Shifts");
        }

        [Route("tasks")]
        public IActionResult Tasks()
        {
            return RedirectToAction("Shifts");
        }

        [Route("maintenance")]
        public IActionResult Maintenance()
        {
            return View();
        }
    }
}
