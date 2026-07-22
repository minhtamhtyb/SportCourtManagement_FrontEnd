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
        public async Task<IActionResult> Index([FromQuery] int? complexId = null, [FromQuery] string? status = null, [FromQuery] int page = 1)
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

            page = page < 1 ? 1 : page;
            int pageSize = 100;
            var model = new StaffMaintenanceViewModel();

            string url = $"{_baseUrl.TrimEnd('/')}/api/manager/complexes/{selectedComplexId}/maintenance?page=1&pageSize={pageSize}";

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

            // Calculate Summary Counts
            model.PendingCount = model.Schedules.Items.Count(s => s.Status == "Pending" || s.Status == "Scheduled");
            model.InProgressCount = model.Schedules.Items.Count(s => s.Status == "InProgress");
            model.CompletedCount = model.Schedules.Items.Count(s => s.Status == "Completed");
            model.CancelledCount = model.Schedules.Items.Count(s => s.Status == "Cancelled");

            // Filter status if requested
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
            pageSize = 10;
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
            ViewBag.SelectedComplexId = selectedComplexId;
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
            [FromForm] string? proofImageUrl,
            [FromForm] IFormFile? proofImageFile,
            [FromForm] int? assignedStaffId,
            [FromQuery] string? status = null, 
            [FromQuery] int page = 1)
        {
            AttachAuthToken();

            string finalImageUrl = proofImageUrl?.Trim() ?? string.Empty;

            if (proofImageFile != null && proofImageFile.Length > 0)
            {
                var uploadedUrl = await UploadImageToCloudinaryAsync(proofImageFile);
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    finalImageUrl = uploadedUrl;
                }
            }

            if (string.IsNullOrWhiteSpace(resultNote))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mô tả kết quả hoàn thành bảo trì.";
                return RedirectToAction(nameof(Index), new { status, page });
            }



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
                result = resultNote.Trim(),
                imageProof = string.IsNullOrWhiteSpace(finalImageUrl) ? null : finalImageUrl,
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

        private async Task<string?> UploadImageToCloudinaryAsync(IFormFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "image/jpeg");
                content.Add(streamContent, "file", file.FileName);

                AttachAuthToken();
                var response = await _client.PostAsync($"{_baseUrl.TrimEnd('/')}/api/complexes/upload-image", content);
                if (response.IsSuccessStatusCode)
                {
                    string jsonStr = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonStr);
                    if (doc.RootElement.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("url", out var urlProp))
                    {
                        return urlProp.GetString();
                    }
                    if (doc.RootElement.TryGetProperty("url", out var directUrlProp))
                    {
                        return directUrlProp.GetString();
                    }
                }
            }
            catch { }
            return null;
        }
    }
}


