using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Manager;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("manager/tasks")]
    public class TaskController : Controller
    {
        private readonly HttpClient _client;
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
                return $"https://localhost:7075/api/manager/complexes/{complexId}";
            }
        }
        private readonly JsonSerializerOptions _jsonOpts;

        public TaskController()
        {
            _client = new HttpClient();
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── GET: Support multiple routes for ease of use
        [HttpGet]
        public async Task<IActionResult> Tasks()
        {
            await LoadLayoutDataAsync();
            var model = new TaskViewModel();
            

            // Fetch tasks list (all tasks by using a large page size)
            var taskResponse = await _client.GetAsync(_apiBase + "/tasks?pageSize=100");
            if (taskResponse.IsSuccessStatusCode)
            {
                string raw = await taskResponse.Content.ReadAsStringAsync();
                var pagedResult = JsonSerializer.Deserialize<PagedTaskResponse>(raw, _jsonOpts);
                if (pagedResult != null)
                {
                    model.Tasks = pagedResult;
                }
            }

            // Calculate stats
            model.PendingCount = model.Tasks.Items.Count(t => t.Status == "Pending");
            model.InProgressCount = model.Tasks.Items.Count(t => t.Status == "InProgress");
            model.CompletedCount = model.Tasks.Items.Count(t => t.Status == "Completed");
            model.ApprovedCount = model.Tasks.Items.Count(t => t.Status == "Approved");

            // Fetch staff members for complex 1
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

        // ── POST: /manager/tasks/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTask(CreateTaskRequest model)
        {
            AttachAuthToken();
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Tasks));
            }

            
            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_apiBase + "/tasks", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo công việc thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Không thể tạo công việc: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể tạo công việc. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể tạo công việc. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction(nameof(Tasks));
        }

        // ── POST: /manager/tasks/update
        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTask([FromForm] int TaskId, UpdateTaskRequest model)
        {
            AttachAuthToken();
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Tasks));
            }

            
            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(_apiBase + $"/tasks/{TaskId}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật công việc thành công!";
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

            return RedirectToAction(nameof(Tasks));
        }

        // ── POST: /manager/tasks/verify
        [HttpPost("verify")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyTask([FromForm] int TaskId, VerifyTaskRequest model)
        {
            AttachAuthToken();
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu xác nhận không hợp lệ.";
                return RedirectToAction(nameof(Tasks));
            }

            
            var json = JsonSerializer.Serialize(model, _jsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PutAsync(_apiBase + $"/tasks/{TaskId}/verify", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = model.IsApproved == true ? "Đã duyệt nghiệm thu công việc!" : "Đã từ chối công việc nghiệm thu!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không thể xác nhận công việc. Lỗi: {response.StatusCode}";
            }

            return RedirectToAction(nameof(Tasks));
        }

        // ── POST: /manager/tasks/delete
        [HttpPost("delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask([FromForm] int TaskId)
        {
            AttachAuthToken();
            if (TaskId <= 0)
            {
                TempData["ErrorMessage"] = "Mã công việc không hợp lệ.";
                return RedirectToAction(nameof(Tasks));
            }

            
            var response = await _client.DeleteAsync(_apiBase + $"/tasks/{TaskId}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Xoá công việc thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Không thể xoá: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể xoá công việc. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể xoá công việc. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction(nameof(Tasks));
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
                var response = await _client.GetAsync("https://localhost:7075/api/complexes?pageSize=100");
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
