using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Authorize(Roles = "Staff")]
    [Route("staff/schedule")]
    public class StaffScheduleController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOpts;

        public StaffScheduleController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _client = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7075";
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // GET: /staff/schedule
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? weekStart = null, [FromQuery] int? complexId = null, [FromQuery] string filter = "my")
        {
            await LoadLayoutDataAsync();
            AttachAuthToken();

            if (complexId.HasValue && complexId.Value > 0)
            {
                HttpContext.Session.SetInt32("selected_complex_id", complexId.Value);
            }
            else if (int.TryParse(Request.Query["complexId"], out int qId) && qId > 0)
            {
                HttpContext.Session.SetInt32("selected_complex_id", qId);
            }

            int selectedComplexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;

            DateTime inputDate;
            if (string.IsNullOrEmpty(weekStart) || !DateTime.TryParse(weekStart, out inputDate))
            {
                inputDate = GetVietnamTime();
            }

            int diffToMonday = (7 + (inputDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime mondayDate = inputDate.AddDays(-diffToMonday).Date;
            DateTime sundayDate = mondayDate.AddDays(6);
            string formattedWeekStart = mondayDate.ToString("yyyy-MM-dd");

            var weeklyData = new WeeklyScheduleResponse();
            string url = $"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{selectedComplexId}/staff/shifts/my?weekStart={formattedWeekStart}";

            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string rawJson = await response.Content.ReadAsStringAsync();
                weeklyData = JsonSerializer.Deserialize<WeeklyScheduleResponse>(rawJson, _jsonOpts) ?? new WeeklyScheduleResponse();
            }

            // User Identity info
            string userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "";
            string userName = User.Identity?.Name ?? "Nhân viên";

            var myShifts = weeklyData.Days?.SelectMany(d => d.Shifts).ToList() ?? new List<SportCourtManagement_FrontEnd.Models.Manager.StaffShiftResponse>();

            string todayStr = DateOnly.FromDateTime(GetVietnamTime()).ToString("yyyy-MM-dd");
            var myTodayShift = myShifts.FirstOrDefault(s => s.ShiftDate == todayStr);

            ViewBag.UserEmail = userEmail;
            ViewBag.UserName = userName;
            ViewBag.SelectedComplexId = selectedComplexId;
            ViewBag.WeekStart = mondayDate.ToString("dd/MM/yyyy");
            ViewBag.WeekEnd = sundayDate.ToString("dd/MM/yyyy");
            ViewBag.RawWeekStart = formattedWeekStart;
            ViewBag.PrevWeekStart = mondayDate.AddDays(-7).ToString("yyyy-MM-dd");
            ViewBag.NextWeekStart = mondayDate.AddDays(7).ToString("yyyy-MM-dd");
            var todayVn = GetVietnamTime().Date;
            ViewBag.IsCurrentWeek = (mondayDate == todayVn.AddDays(-(7 + (todayVn.DayOfWeek - DayOfWeek.Monday)) % 7).Date);
            ViewBag.Filter = filter;

            // Summary Stats for logged in staff
            ViewBag.MyTotalShifts = myShifts.Count;
            ViewBag.MyCompletedShifts = myShifts.Count(s => s.CheckOutTime.HasValue);
            ViewBag.MyWorkHours = myShifts.Count * 8;
            ViewBag.MyTodayShift = myTodayShift;

            return View(weeklyData);
        }

        // POST: /staff/schedule/check-in
        [HttpPost("check-in")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn([FromForm] int shiftId, [FromForm] string? weekStart = null, [FromForm] string filter = "my")
        {
            AttachAuthToken();
            int complexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;

            var response = await _client.PostAsync($"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{complexId}/staff/shifts/{shiftId}/check-in", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Chấm công VÀO ca trực thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = ParseErrorMessage(rawError, "Không thể thực hiện chấm công vào.");
            }

            return RedirectToAction(nameof(Index), new { weekStart, filter });
        }

        // POST: /staff/schedule/check-out
        [HttpPost("check-out")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut([FromForm] int shiftId, [FromForm] string? weekStart = null, [FromForm] string filter = "my")
        {
            AttachAuthToken();
            int complexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;

            var response = await _client.PostAsync($"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{complexId}/staff/shifts/{shiftId}/check-out", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Chấm công RA ca trực thành công! Hoàn thành ca làm việc.";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = ParseErrorMessage(rawError, "Không thể thực hiện chấm công ra.");
            }

            return RedirectToAction(nameof(Index), new { weekStart, filter });
        }

        private string ParseErrorMessage(string rawError, string fallback)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawError);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                {
                    return msgProp.GetString() ?? fallback;
                }
            }
            catch { }
            return fallback;
        }

        private void AttachAuthToken()
        {
            HttpContext.Session.LoadAsync().GetAwaiter().GetResult();
            var token = HttpContext.Session.GetString(Services.Api.JwtForwardingHandler.SessionTokenKey);

            if (string.IsNullOrEmpty(token))
                token = User.FindFirst(Services.Api.JwtForwardingHandler.AccessTokenClaimType)?.Value;

            _client.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrEmpty(token))
                _client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        private async Task LoadLayoutDataAsync()
        {
            AttachAuthToken();
            try
            {
                var response = await _client.GetAsync($"{_baseUrl.TrimEnd('/')}/api/complexes?pageSize=100");
                if (response.IsSuccessStatusCode)
                {
                    string raw = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("items", out var itemsProp))
                    {
                        ViewBag.Complexes = JsonSerializer.Deserialize<List<object>>(itemsProp.GetRawText());
                    }
                }
            }
            catch { }
        }
        private static DateTime GetVietnamTime()
        {
            var utcNow = DateTime.UtcNow;
            TimeZoneInfo vnZone;
            try
            {
                vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                vnZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vnZone);
        }
    }
}
