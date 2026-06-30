using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models;
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
    [Route("manager/maintenance")]
    public class MaintenanceController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _apiBase = "https://localhost:7075/api/manager/complexes/1";
        private readonly JsonSerializerOptions _jsonOpts;

        public MaintenanceController()
        {
            _client = new HttpClient();
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── GET: /manager/maintenance ───────────────────
        [HttpGet]
        public async Task<IActionResult> Maintenance()
        {
            var model = new MaintenanceViewModel();

            var scheduleResponse = await _client.GetAsync(_apiBase + "/maintenance?pageSize=100");
            if (scheduleResponse.IsSuccessStatusCode)
            {
                string raw = await scheduleResponse.Content.ReadAsStringAsync();
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

            var staffResponse = await _client.GetAsync(_apiBase + "/staff?pageSize=100");
            if (staffResponse.IsSuccessStatusCode)
            {
                string raw = await staffResponse.Content.ReadAsStringAsync();
                var pagedStaff = JsonSerializer.Deserialize<PagedStaffResponse>(raw, _jsonOpts);
                if (pagedStaff != null)
                {
                    model.Staffs = pagedStaff.Items;
                }
            }

            return View(model);
        }

        // ── POST: /manager/maintenance/create ─────────────────────────────────────────
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaintenance(CreateMaintenanceRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Maintenance));
            }

            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_apiBase + "/maintenance", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo lịch bảo trì thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Không thể tạo lịch bảo trì: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể tạo lịch bảo trì. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể tạo lịch bảo trì. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction("Maintenance");
        }

        // ── POST: /manager/maintenance/update ─────────────────────────────────────────
        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMaintenance([FromForm] int MaintenanceId, UpdateMaintenanceRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Maintenance));
            }

            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(_apiBase + $"/maintenance/{MaintenanceId}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật lịch bảo trì thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Không thể cập nhật: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể cập nhật. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể cập nhật. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction("Maintenance");
        }

        // ── POST: /manager/maintenance/verify ─────────────────────────────────────────
        [HttpPost("verify")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyMaintenance([FromForm] int MaintenanceId, VerifyMaintenanceRequest model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu xác nhận không hợp lệ.";
                return RedirectToAction(nameof(Maintenance));
            }

            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(_apiBase + $"/maintenance/{MaintenanceId}/verify", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = model.IsApproved == true ? "Đã duyệt lịch bảo trì thành công!" : "Đã từ chối lịch bảo trì!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể xác nhận lịch bảo trì. Lỗi: {response.StatusCode}";
            }

            return RedirectToAction("Maintenance");
        }

        // ── POST: /manager/maintenance/delete ─────────────────────────────────────────
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaintenance([FromForm] int MaintenanceId)
        {
            if (MaintenanceId <= 0)
            {
                TempData["ErrorMessage"] = "Mã lịch bảo trì không hợp lệ.";
                return RedirectToAction(nameof(Maintenance));
            }

            var response = await _client.DeleteAsync(_apiBase + $"/maintenance/{MaintenanceId}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Xoá lịch bảo trì thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể xoá lịch bảo trì. Lỗi: {response.StatusCode}";
            }

            return RedirectToAction("Maintenance");
        }
    }
}
