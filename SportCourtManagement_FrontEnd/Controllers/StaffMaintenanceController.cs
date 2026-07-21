using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Authorize(Roles = "Staff")]
    [Route("staff/maintenance")]
    public class StaffMaintenanceController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOpts;

        public StaffMaintenanceController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _client = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7075";
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private string GetApiBase()
        {
            int complexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;
            return $"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{complexId}";
        }

        // ── GET: /staff/maintenance ───────────────────
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? status = null, [FromQuery] int page = 1)
        {
            await LoadLayoutDataAsync();
            AttachAuthToken();

            page = page < 1 ? 1 : page;
            int pageSize = 10;
            var model = new StaffMaintenanceViewModel();

            string url = $"{GetApiBase()}/maintenance?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }

            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                var pagedResult = JsonSerializer.Deserialize<PagedMaintenanceResponse>(raw, _jsonOpts);
                if (pagedResult != null)
                {
                    model.Schedules = pagedResult;
                }
            }

            model.PendingCount = model.Schedules.Items.Count(s => s.Status == "Pending" || s.Status == "Scheduled");
            model.InProgressCount = model.Schedules.Items.Count(s => s.Status == "InProgress");
            model.CompletedCount = model.Schedules.Items.Count(s => s.Status == "Completed");
            model.CancelledCount = model.Schedules.Items.Count(s => s.Status == "Cancelled");

            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            return View(model);
        }

        // ── POST: /staff/maintenance/{id}/start ───────────────────
        [HttpPost("{id:int}/start")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartMaintenance(
            int id, 
            [FromForm] int courtId, 
            [FromForm] string maintenanceType, 
            [FromForm] string startDateTime, 
            [FromForm] string endDateTime, 
            [FromForm] string reason, 
            [FromForm] int? assignedStaffId,
            [FromQuery] string? status = null, 
            [FromQuery] int page = 1)
        {
            AttachAuthToken();
            int complexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;

            DateTime start = DateTime.TryParse(startDateTime, out var sParsed) ? sParsed : DateTime.Now;
            DateTime end = DateTime.TryParse(endDateTime, out var eParsed) ? eParsed : DateTime.Now.AddHours(2);

            var updateReq = new
            {
                courtId = courtId,
                maintenanceType = ParseMaintenanceTypeString(maintenanceType),
                startDateTime = start,
                endDateTime = end,
                reason = reason,
                assignedStaffId = assignedStaffId,
                status = "InProgress"
            };

            var json = JsonSerializer.Serialize(updateReq, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{complexId}/maintenance/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Bắt đầu thực hiện công việc bảo trì sân thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = ParseErrorMessage(rawError, "Không thể bắt đầu bảo trì.");
            }

            return RedirectToAction(nameof(Index), new { status, page });
        }

        // ── POST: /staff/maintenance/{id}/complete ───────────────────
        [HttpPost("{id:int}/complete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteMaintenance(
            int id, 
            [FromForm] int courtId, 
            [FromForm] string maintenanceType, 
            [FromForm] string startDateTime, 
            [FromForm] string endDateTime, 
            [FromForm] string reason, 
            [FromForm] string resultNote, 
            [FromForm] int? assignedStaffId,
            [FromQuery] string? status = null, 
            [FromQuery] int page = 1)
        {
            AttachAuthToken();
            int complexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;

            DateTime start = DateTime.TryParse(startDateTime, out var sParsed) ? sParsed : DateTime.Now;
            DateTime end = DateTime.TryParse(endDateTime, out var eParsed) ? eParsed : DateTime.Now.AddHours(2);

            var updateReq = new
            {
                courtId = courtId,
                maintenanceType = ParseMaintenanceTypeString(maintenanceType),
                startDateTime = start,
                endDateTime = end,
                reason = reason,
                result = string.IsNullOrWhiteSpace(resultNote) ? "Báo cáo hoàn thành công việc bảo trì từ nhân viên." : resultNote,
                assignedStaffId = assignedStaffId,
                status = "Completed"
            };

            var json = JsonSerializer.Serialize(updateReq, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync($"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{complexId}/maintenance/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã gửi báo cáo hoàn thành bảo trì! Chờ Quản lý nghiệm thu.";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = ParseErrorMessage(rawError, "Không thể gửi báo cáo hoàn thành.");
            }

            return RedirectToAction(nameof(Index), new { status, page });
        }

        private string ParseMaintenanceTypeString(string typeStr)
        {
            if (string.IsNullOrWhiteSpace(typeStr)) return "Routine";
            var normalized = typeStr.Trim();
            return normalized switch
            {
                "1" => "Emergency",
                "2" => "Upgrade",
                "0" => "Routine",
                "emergency" => "Emergency",
                "upgrade" => "Upgrade",
                "routine" => "Routine",
                _ => char.ToUpper(normalized[0]) + normalized.Substring(1)
            };
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
    }
}
