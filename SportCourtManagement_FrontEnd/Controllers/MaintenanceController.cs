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
    [Authorize(Roles = "Manager,Admin")]
    [Route("manager/maintenance")]
    public class MaintenanceController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private string _apiBase
        {
            get
            {
                int complexId = 1;
                if (int.TryParse(Request.Query["complexId"], out var qId) && qId > 0)
                {
                    complexId = qId;
                    HttpContext.Session.LoadAsync().GetAwaiter().GetResult();
                    HttpContext.Session.SetInt32("selected_complex_id", qId);
                    HttpContext.Session.CommitAsync().GetAwaiter().GetResult();
                }
                else
                {
                    HttpContext.Session.LoadAsync().GetAwaiter().GetResult();
                    complexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;
                }
                return $"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{complexId}";
            }
        }
        private readonly JsonSerializerOptions _jsonOpts;

        public MaintenanceController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _client = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7075";
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── GET: /manager/maintenance ───────────────────
        [HttpGet]
        public async Task<IActionResult> Maintenance([FromQuery] string? status = null, [FromQuery] int page = 1)
        {
            await LoadLayoutDataAsync();
            var model = new MaintenanceViewModel();
            page = page < 1 ? 1 : page;

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

            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    model.Schedules.Items = model.Schedules.Items
                        .Where(s => s.Status.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) || s.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    model.Schedules.Items = model.Schedules.Items
                        .Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            // Paginate strictly 10 items per page
            int pageSize = 10;
            int totalItems = model.Schedules.Items.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            totalPages = totalPages > 0 ? totalPages : 1;
            page = page > totalPages ? totalPages : (page < 1 ? 1 : page);

            var pagedItems = model.Schedules.Items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            model.Schedules.Items = pagedItems;
            model.Schedules.Page = page;
            model.Schedules.PageSize = pageSize;
            model.Schedules.TotalCount = totalItems;
            model.Schedules.TotalPages = totalPages;

            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;

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

            var courtResponse = await _client.GetAsync(_apiBase + "/maintenance/courts");
            if (courtResponse.IsSuccessStatusCode)
            {
                string raw = await courtResponse.Content.ReadAsStringAsync();
                var courts = JsonSerializer.Deserialize<List<CourtInfoResponse>>(raw, _jsonOpts);
                if (courts != null)
                {
                    model.Courts = courts;
                }
            }

            return View(model);
        }


        // ── POST: /manager/maintenance/create ─────────────────────────────────────────
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaintenance(CreateMaintenanceRequest model)
        {
            AttachAuthToken();
            if (!model.AssignedStaffId.HasValue || model.AssignedStaffId.Value <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn nhân viên phụ trách.";
                return RedirectToAction(nameof(Maintenance));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Maintenance));
            }

            if (model.StartDateTime >= model.EndDateTime)
            {
                TempData["ErrorMessage"] = "Thời gian bắt đầu phải trước thời gian kết thúc.";
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
            AttachAuthToken();
            if (!model.AssignedStaffId.HasValue || model.AssignedStaffId.Value <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn nhân viên phụ trách.";
                return RedirectToAction(nameof(Maintenance));
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Maintenance));
            }

            if (model.StartDateTime >= model.EndDateTime)
            {
                TempData["ErrorMessage"] = "Thời gian bắt đầu phải trước thời gian kết thúc.";
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
            AttachAuthToken();
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
            AttachAuthToken();
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

            int complexId = 1;
            if (int.TryParse(Request.Query["complexId"], out var qId) && qId > 0)
            {
                complexId = qId;
                await HttpContext.Session.LoadAsync();
                HttpContext.Session.SetInt32("selected_complex_id", qId);
                await HttpContext.Session.CommitAsync();
            }
            else
            {
                await HttpContext.Session.LoadAsync();
                complexId = HttpContext.Session.GetInt32("selected_complex_id") ?? 1;
            }

            ViewBag.CurrentComplexId = complexId;

            try
            {
                var response = await _client.GetAsync($"{_baseUrl.TrimEnd('/')}/api/complexes?pageSize=100");
                if (response.IsSuccessStatusCode)
                {
                    var raw = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(raw);
                    if (doc.RootElement.TryGetProperty("data", out var dataProp))
                    {
                        if (dataProp.TryGetProperty("items", out var itemsProp))
                        {
                            var complexes = JsonSerializer.Deserialize<List<SportCourtManagement_FrontEnd.Models.DTOs.CourtComplexDto>>(itemsProp.GetRawText(), _jsonOpts);
                            ViewBag.Complexes = complexes;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }

            if (ViewBag.Complexes == null)
            {
                ViewBag.Complexes = new List<SportCourtManagement_FrontEnd.Models.DTOs.CourtComplexDto>();
            }
        }
    }
}
