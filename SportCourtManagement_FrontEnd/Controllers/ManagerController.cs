using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Manager;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Authorize(Roles = "Manager")]
    [Route("manager")]
    public class ManagerController : Controller
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

        public ManagerController(IHttpContextAccessor httpContextAccessor)
        {
            _client = new HttpClient();
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── GET: /manager ─────────────────────────────────────────────────────
        [HttpGet("")]
        public IActionResult Index() => RedirectToAction(nameof(Shifts));

        // ── GET: /manager/staff/shifts ────────────────────────────────────────
        [HttpGet("staff/shifts")]
        public async Task<IActionResult> Shifts([FromQuery] string? weekStart = null)
        {
            await LoadLayoutDataAsync();
            if (string.IsNullOrEmpty(weekStart))
            {
                weekStart = DateTime.Today.ToString("yyyy-MM-dd");
            }

            var weeklyData = new WeeklyScheduleResponse();
            var url = _apiBase + "/staff/shifts/weekly";
            if (!string.IsNullOrEmpty(weekStart))
            {
                url += $"?weekStart={weekStart}";
            }
            var response = await _client.GetAsync(url);
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
            AttachAuthToken();
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
            AttachAuthToken();
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
            AttachAuthToken();
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
        public async Task<IActionResult> Attendance(
            [FromQuery] string? dateFrom = null, 
            [FromQuery] string? dateTo = null, 
            [FromQuery] int? staffId = null,
            [FromQuery] string? shiftType = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1)
        {
            await LoadLayoutDataAsync();
            var model = new AttendanceViewModel();
            
            var today = DateTime.Today;
            var defaultFrom = new DateTime(today.Year, today.Month, 1).ToString("yyyy-MM-dd");
            var defaultTo = today.ToString("yyyy-MM-dd");

            model.DateFrom = string.IsNullOrEmpty(dateFrom) ? defaultFrom : dateFrom;
            model.DateTo = string.IsNullOrEmpty(dateTo) ? defaultTo : dateTo;
            model.SelectedStaffId = staffId;
            model.SelectedShiftType = shiftType;
            model.SearchQuery = search;
            model.CurrentPage = page < 1 ? 1 : page;

            // Fetch staff list for dropdown filter
            var staffResponse = await _client.GetAsync(_apiBase + "/staff?pageSize=100");
            if (staffResponse.IsSuccessStatusCode)
            {
                string rawStaff = await staffResponse.Content.ReadAsStringAsync();
                var pagedStaff = JsonSerializer.Deserialize<PagedStaffResponse>(rawStaff, _jsonOpts);
                if (pagedStaff != null && pagedStaff.Items != null)
                {
                    model.Staffs = pagedStaff.Items.Where(s => s.IsActive).ToList();
                }
            }

            // Fetch attendance report
            var queryParams = $"?dateFrom={model.DateFrom}&dateTo={model.DateTo}";
            if (model.SelectedStaffId.HasValue)
            {
                queryParams += $"&staffId={model.SelectedStaffId.Value}";
            }

            var reportResponse = await _client.GetAsync(_apiBase + "/staff/attendance" + queryParams);
            var allRecords = new List<StaffShiftResponse>();
            if (reportResponse.IsSuccessStatusCode)
            {
                string rawReport = await reportResponse.Content.ReadAsStringAsync();
                allRecords = JsonSerializer.Deserialize<List<StaffShiftResponse>>(rawReport, _jsonOpts) ?? new List<StaffShiftResponse>();
            }

            // Filter in-memory by search and shift type
            var filtered = allRecords.AsEnumerable();

            if (!string.IsNullOrEmpty(model.SearchQuery))
            {
                var q = model.SearchQuery.Trim().ToLower();
                filtered = filtered.Where(r => 
                    r.StaffName.ToLower().Contains(q) || 
                    r.StaffEmail.ToLower().Contains(q) || 
                    r.StaffId.ToString().Contains(q));
            }

            if (!string.IsNullOrEmpty(model.SelectedShiftType))
            {
                filtered = filtered.Where(r => r.ShiftType.Equals(model.SelectedShiftType, StringComparison.OrdinalIgnoreCase));
            }

            var filteredList = filtered.ToList();

            // Calculate statistics before pagination
            model.OnTimeCount = filteredList.Count(r => r.CheckInTime.HasValue && r.LateMinutes == 0);
            model.LateCount = filteredList.Count(r => r.CheckInTime.HasValue && r.LateMinutes > 0);

            // Paginate
            model.TotalRecords = filteredList.Count;
            model.TotalPages = (int)Math.Ceiling(model.TotalRecords / (double)model.PageSize);
            if (model.TotalPages < 1) model.TotalPages = 1;

            if (model.CurrentPage > model.TotalPages) model.CurrentPage = model.TotalPages;

            model.Records = filteredList
                .Skip((model.CurrentPage - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToList();

            return View(model);
        }

        [HttpGet("staff/list")]
        public async Task<IActionResult> StaffList([FromQuery] string? search = null, [FromQuery] int page = 1)
        {
            await LoadLayoutDataAsync();
            var model = new PagedStaffResponse();
            int pageSize = 10;
            page = page < 1 ? 1 : page;

            var response = await _client.GetAsync(_apiBase + $"/staff?search={search}&page={page}&pageSize={pageSize}");
            if (response.IsSuccessStatusCode)
            {
                string rawJson = await response.Content.ReadAsStringAsync();
                model = JsonSerializer.Deserialize<PagedStaffResponse>(rawJson, _jsonOpts) ?? new PagedStaffResponse();
            }

            ViewData["SearchQuery"] = search;
            return View(model);
        }

        [HttpPost("staff/list/assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignStaff([FromForm] int staffId)
        {
            AttachAuthToken();
            if (staffId <= 0)
            {
                TempData["ErrorMessage"] = "ID nhân viên không hợp lệ.";
                return RedirectToAction("StaffList");
            }

            var response = await _client.PostAsync(_apiBase + $"/staff/{staffId}/assign", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Gán nhân viên vào cơ sở thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Không thể gán nhân viên: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể gán nhân viên. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể gán nhân viên. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction("StaffList");
        }

        [HttpPost("staff/list/unassign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignStaff([FromForm] int staffId)
        {
            AttachAuthToken();
            if (staffId <= 0)
            {
                TempData["ErrorMessage"] = "ID nhân viên không hợp lệ.";
                return RedirectToAction("StaffList");
            }

            var response = await _client.DeleteAsync(_apiBase + $"/staff/{staffId}/unassign");
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Gỡ nhân viên khỏi cơ sở thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Không thể gỡ nhân viên: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể gỡ nhân viên. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể gỡ nhân viên. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction("StaffList");
        }

        [HttpGet("staff/salary")]
        public async Task<IActionResult> SalaryConfig()
        {
            await LoadLayoutDataAsync();
            return View();
        }

        [HttpGet("dashboard")]
        public IActionResult Dashboard() => RedirectToAction(nameof(Shifts));

        private void AttachAuthToken()
        {
            // AccountController lưu JWT vào Session với key "access_token" (JwtForwardingHandler.SessionTokenKey)
            HttpContext.Session.LoadAsync().GetAwaiter().GetResult();
            var token = HttpContext.Session.GetString(Services.Api.JwtForwardingHandler.SessionTokenKey);

            // Fallback: lấy từ Claims nếu Session chưa có
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
