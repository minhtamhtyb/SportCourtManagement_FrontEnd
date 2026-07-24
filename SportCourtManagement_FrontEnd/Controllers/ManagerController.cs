using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Manager;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SportCourtManagement_FrontEnd.Controllers
{
    [Authorize(Roles = "Manager,Admin")]
    [Route("manager")]
    public class ManagerController : Controller
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

        public ManagerController(IHttpContextAccessor httpContextAccessor, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _client = new HttpClient();
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7075";
            _jsonOpts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        // ── GET: /manager ─────────────────────────────────────────────────────
        [HttpGet("")]
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // ── GET: /manager/staff/shifts ────────────────────────────────────────
        [HttpGet("staff/shifts")]
        public async Task<IActionResult> Shifts([FromQuery] string? weekStart = null)
        {
            await LoadLayoutDataAsync();

            DateTime inputDate;
            if (string.IsNullOrEmpty(weekStart) || !DateTime.TryParse(weekStart, out inputDate))
            {
                inputDate = DateTime.Today;
            }

            int diffToMonday = (7 + (inputDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime mondayDate = inputDate.AddDays(-diffToMonday).Date;
            weekStart = mondayDate.ToString("yyyy-MM-dd");

            var weeklyData = new WeeklyScheduleResponse();
            var url = _apiBase + $"/staff/shifts/weekly?weekStart={weekStart}";
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

        // ── POST: /manager/staff/shifts/{shiftId}/check-in ───────────────────
        [HttpPost("staff/shifts/{shiftId:int}/check-in")]
        public async Task<IActionResult> CheckInStaff(int shiftId, [FromQuery] string? dateFrom = null, [FromQuery] string? dateTo = null, [FromQuery] int? staffId = null, [FromQuery] string? shiftType = null, [FromQuery] string? search = null)
        {
            AttachAuthToken();
            var response = await _client.PostAsync(_apiBase + $"/staff/shifts/{shiftId}/check-in", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Điểm danh vào ca thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Lỗi: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể điểm danh. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể điểm danh. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction("Attendance", new { dateFrom, dateTo, staffId, shiftType, search });
        }

        // ── POST: /manager/staff/shifts/{shiftId}/check-out ──────────────────
        [HttpPost("staff/shifts/{shiftId:int}/check-out")]
        public async Task<IActionResult> CheckOutStaff(int shiftId, [FromQuery] string? dateFrom = null, [FromQuery] string? dateTo = null, [FromQuery] int? staffId = null, [FromQuery] string? shiftType = null, [FromQuery] string? search = null)
        {
            AttachAuthToken();
            var response = await _client.PostAsync(_apiBase + $"/staff/shifts/{shiftId}/check-out", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Điểm danh ra ca thành công!";
            }
            else
            {
                string rawError = await response.Content.ReadAsStringAsync();
                try
                {
                    using var doc = JsonDocument.Parse(rawError);
                    if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        TempData["ErrorMessage"] = $"Lỗi: {msgProp.GetString()}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể điểm danh. Lỗi: {response.StatusCode}";
                    }
                }
                catch
                {
                    TempData["ErrorMessage"] = $"Không thể điểm danh. Lỗi: {response.StatusCode}";
                }
            }

            return RedirectToAction("Attendance", new { dateFrom, dateTo, staffId, shiftType, search });
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

            // Fetch unassigned staff list
            var unassignedResponse = await _client.GetAsync(_apiBase + "/staff/unassigned");
            var unassignedStaff = new List<StaffSummaryResponse>();
            if (unassignedResponse.IsSuccessStatusCode)
            {
                string rawUnassigned = await unassignedResponse.Content.ReadAsStringAsync();
                unassignedStaff = JsonSerializer.Deserialize<List<StaffSummaryResponse>>(rawUnassigned, _jsonOpts) 
                                  ?? new List<StaffSummaryResponse>();
            }
            ViewBag.UnassignedStaff = unassignedStaff;

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

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            await LoadLayoutDataAsync();
            AttachAuthToken();

            int staffCount = 0;
            int todayShiftCount = 0;
            int pendingTaskCount = 0;

            // 1. Get Staff Count
            try
            {
                var response = await _client.GetAsync(_apiBase + "/staff?pageSize=1");
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(rawJson);
                    if (doc.RootElement.TryGetProperty("totalCount", out var totalCountProp))
                    {
                        staffCount = totalCountProp.GetInt32();
                    }
                }
            }
            catch { }

            // 2. Get Today's Shifts Count
            try
            {
                var response = await _client.GetAsync(_apiBase + "/staff/schedule");
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    var schedule = JsonSerializer.Deserialize<WeeklyScheduleResponse>(rawJson, _jsonOpts);
                    if (schedule != null && schedule.Days != null)
                    {
                        string todayStr = DateTime.Today.ToString("dd/MM/yyyy");
                        var todayDay = schedule.Days.FirstOrDefault(d => d.Date == todayStr);
                        if (todayDay != null && todayDay.Shifts != null)
                        {
                            todayShiftCount = todayDay.Shifts.Count;
                        }
                    }
                }
            }
            catch { }

            // 3. Get Pending Tasks Count
            try
            {
                var response = await _client.GetAsync(_apiBase + "/tasks?pageSize=100");
                if (response.IsSuccessStatusCode)
                {
                    string rawJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(rawJson);
                    if (doc.RootElement.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsProp.EnumerateArray())
                        {
                            if (item.TryGetProperty("status", out var statusProp))
                            {
                                string? status = statusProp.GetString();
                                if (status == "Pending" || status == "InProgress")
                                {
                                    pendingTaskCount++;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            ViewBag.StaffCount = staffCount;
            ViewBag.TodayShiftCount = todayShiftCount;
            ViewBag.PendingTaskCount = pendingTaskCount;

            return View();
        }

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
