using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Route("manager")]
    public class ManagerController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBase = "https://localhost:7075/api/manager/complexes/1";
        private readonly JsonSerializerOptions _jsonOpts;

        public ManagerController()
        {
            _client = new HttpClient();
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── GET: /manager/staff/shifts ────────────────────────────────────────
        [HttpGet("staff/shifts")]
        public async Task<IActionResult> Shifts()
        {
            var weeklyData = new WeeklyScheduleResponse();
            var response = await _client.GetAsync(_apiBase + "/staff/shifts/weekly");
            if (response.IsSuccessStatusCode)
            {
                string rawJson = await response.Content.ReadAsStringAsync();
                weeklyData = JsonSerializer.Deserialize<WeeklyScheduleResponse>(rawJson, _jsonOpts)
                             ?? new WeeklyScheduleResponse();
            }

            var staffResponse = await _client.GetAsync(_apiBase + "/staff?pageSize=100");
            if (staffResponse.IsSuccessStatusCode)
            {
                string rawStaff = await staffResponse.Content.ReadAsStringAsync();
                var pagedStaff = JsonSerializer.Deserialize<PagedStaffResponse>(rawStaff, _jsonOpts);
                if (pagedStaff != null && pagedStaff.Items != null)
                {
                    weeklyData.Staffs = pagedStaff.Items.Where(s => s.IsActive).ToList();
                }
            }

            if (weeklyData.Staffs == null)
            {
                weeklyData.Staffs = new List<StaffSummaryResponse>();
            }

            var shiftsStaffs = weeklyData.Days
                .SelectMany(d => d.Shifts)
                .GroupBy(s => s.StaffId)
                .Select(g => g.First())
                .ToList();

            foreach (var ss in shiftsStaffs)
            {
                if (!weeklyData.Staffs.Any(s => s.UserId == ss.StaffId))
                {
                    weeklyData.Staffs.Add(new StaffSummaryResponse
                    {
                        UserId = ss.StaffId,
                        FullName = ss.StaffName,
                        Email = ss.StaffEmail,
                        AvatarUrl = ss.AvatarUrl,
                        IsActive = true
                    });
                }
            }

            return View(weeklyData);
        }

        // ── POST: /manager/staff/shifts/create ───────────────────────────────
        [HttpPost("staff/shifts/create")]
        public async Task<IActionResult> CreateShift(CreateShiftRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Shifts");
            }

            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_apiBase + "/staff/shifts", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo ca trực thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể tạo ca trực. Lỗi: {response.StatusCode}";
            }


            return RedirectToAction("Shifts");
        }

        // ── POST: /manager/staff/shifts/update ───────────────────────────────
        [HttpPost("staff/shifts/update")]
        public async Task<IActionResult> UpdateShift([FromForm] int ShiftId, UpdateShiftRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Shifts");
            }

            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(_apiBase + $"/staff/shifts/{ShiftId}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật ca trực thành công!";
            }

            else
            {
                TempData["ErrorMessage"] = $"Không thể cập nhật ca trực. Lỗi: {response.StatusCode}";
            }
            return RedirectToAction("Shifts");
        }

        // ── POST: /manager/staff/shifts/delete ───────────────────────────────
        [HttpPost("staff/shifts/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShift([FromForm] int ShiftId)
        {
            if (ShiftId <= 0)
            {
                TempData["ErrorMessage"] = "ID ca trực không hợp lệ.";
                return RedirectToAction("Shifts");
            }

            var response = await _client.DeleteAsync(_apiBase + $"/staff/shifts/{ShiftId}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Xoá ca trực thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể xoá ca trực. Lỗi: {response.StatusCode}";
            }

            return RedirectToAction("Shifts");
        }


        // ── Other routes ─────────────────────────────────────────────────────
        [HttpGet("staff/attendance")]
        public IActionResult Attendance() => View();

        [HttpGet("staff/list")]
        public IActionResult StaffList() => View();

        [HttpGet("staff/salary")]
        public IActionResult SalaryConfig() => View();

        [HttpGet("dashboard")]
        public IActionResult Dashboard() => RedirectToAction(nameof(Shifts));

        [HttpGet("tasks")]
        public IActionResult Tasks() => RedirectToAction(nameof(Shifts));

    }
}
