using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Manager;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Security.Claims;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Authorize(Roles = "Staff,Admin")]
    [Route("staff/tasks")]
    public class StaffTaskController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly string _apiBase;
        private readonly JsonSerializerOptions _jsonOpts;

        public StaffTaskController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _client = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7075";
            _apiBase = $"{_baseUrl.TrimEnd('/')}/api/staff/tasks";
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // GET: /staff/tasks
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? status = null, [FromQuery] int page = 1)
        {
            await LoadLayoutDataAsync();
            AttachAuthToken();

            page = page < 1 ? 1 : page;
            int pageSize = 100;

            string url = $"{_apiBase}?page=1&pageSize={pageSize}";

            var model = new PagedTaskResponse();
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                model = JsonSerializer.Deserialize<PagedTaskResponse>(raw, _jsonOpts) ?? new PagedTaskResponse();
            }

            ViewBag.PendingCount = model.Items.Count(t => t.Status == "Pending");
            ViewBag.InProgressCount = model.Items.Count(t => t.Status == "InProgress");
            ViewBag.CompletedCount = model.Items.Count(t => t.Status == "Completed");
            ViewBag.ApprovedCount = model.Items.Count(t => t.Status == "Approved");

            if (!string.IsNullOrEmpty(status))
            {
                model.Items = model.Items.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Paginate strictly 10 items per page
            pageSize = 10;
            int totalItems = model.Items.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            totalPages = totalPages > 0 ? totalPages : 1;
            page = page > totalPages ? totalPages : (page < 1 ? 1 : page);

            var pagedItems = model.Items
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            model.Items = pagedItems;
            model.Page = page;
            model.PageSize = pageSize;
            model.TotalCount = totalItems;
            model.TotalPages = totalPages;

            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            return View(model);
        }


        // POST: /staff/tasks/{taskId}/start
        [HttpPost("{taskId:int}/start")]
        public async Task<IActionResult> StartTask(int taskId, [FromQuery] string? status = null, [FromQuery] int page = 1)
        {
            AttachAuthToken();
            var response = await _client.PutAsync($"{_apiBase}/{taskId}/start", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Bắt đầu thực hiện công việc thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = ParseErrorMessage(rawError, "Không thể bắt đầu công việc.");
            }
            return RedirectToAction(nameof(Index), new { status, page });
        }

        // POST: /staff/tasks/{taskId}/complete
        [HttpPost("{taskId:int}/complete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTask(
            int taskId, 
            [FromForm] string resultNote, 
            [FromForm] string? proofImageUrl, 
            [FromForm] IFormFile? proofImageFile,
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
                TempData["ErrorMessage"] = "Vui lòng nhập mô tả kết quả công việc.";
                return RedirectToAction(nameof(Index), new { status, page });
            }



            var payload = new
            {
                resultNote = resultNote.Trim(),
                proofImageUrl = string.IsNullOrWhiteSpace(finalImageUrl) ? null : finalImageUrl
            };


            var json = JsonSerializer.Serialize(payload, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PutAsync($"{_apiBase}/{taskId}/complete", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã gửi báo cáo hoàn thành công việc thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = ParseErrorMessage(rawError, "Không thể hoàn thành công việc.");
            }
            return RedirectToAction(nameof(Index), new { status, page });
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
