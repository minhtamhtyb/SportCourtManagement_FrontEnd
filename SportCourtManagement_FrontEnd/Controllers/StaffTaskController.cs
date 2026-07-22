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
            int pageSize = 10;

            string url = $"{_apiBase}?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }

            var model = new PagedTaskResponse();
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string raw = await response.Content.ReadAsStringAsync();
                model = JsonSerializer.Deserialize<PagedTaskResponse>(raw, _jsonOpts) ?? new PagedTaskResponse();
            }

            ViewBag.SelectedStatus = status;
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
        public async Task<IActionResult> CompleteTask(int taskId, [FromQuery] string? status = null, [FromQuery] int page = 1)
        {
            AttachAuthToken();
            var response = await _client.PutAsync($"{_apiBase}/{taskId}/complete", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã báo cáo hoàn thành công việc!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = ParseErrorMessage(rawError, "Không thể hoàn thành công việc.");
            }
            return RedirectToAction(nameof(Index), new { status, page });
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
