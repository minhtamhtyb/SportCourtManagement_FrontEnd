using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Authorize]
    [Route("Notification")]
    public class NotificationController : Controller
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOpts;

        public NotificationController(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _client = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7075";
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
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

        // GET: /Notification/GetMyNotifications
        [HttpGet("GetMyNotifications")]
        public async Task<IActionResult> GetMyNotifications([FromQuery] int limit = 50)
        {
            AttachAuthToken();
            var url = $"{_baseUrl.TrimEnd('/')}/api/notifications?limit={limit}";

            try
            {
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    return Content(rawJson, "application/json");
                }
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // GET: /Notification/GetUnreadCount
        [HttpGet("GetUnreadCount")]
        public async Task<IActionResult> GetUnreadCount()
        {
            AttachAuthToken();
            var url = $"{_baseUrl.TrimEnd('/')}/api/notifications/unread-count";

            try
            {
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    return Content(rawJson, "application/json");
                }
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // POST: /Notification/MarkAsRead
        [HttpPost("MarkAsRead")]
        public async Task<IActionResult> MarkAsRead([FromQuery] int id)
        {
            AttachAuthToken();
            var url = $"{_baseUrl.TrimEnd('/')}/api/notifications/{id}/read";

            try
            {
                var response = await _client.PutAsync(url, null);
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    return Content(rawJson, "application/json");
                }
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        // POST: /Notification/MarkAllAsRead
        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            AttachAuthToken();
            var url = $"{_baseUrl.TrimEnd('/')}/api/notifications/read-all";

            try
            {
                var response = await _client.PutAsync(url, null);
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    return Content(rawJson, "application/json");
                }
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}
