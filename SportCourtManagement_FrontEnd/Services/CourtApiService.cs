using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Reviews;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Promotions;
using SportCourtManagement_FrontEnd.Models.Tournaments;
using SportCourtManagement_FrontEnd.Models.Services;

namespace SportCourtManagement_FrontEnd.Services;

public class CourtApiService : ICourtApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CourtApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CourtApiService(HttpClient httpClient, ILogger<CourtApiService> logger, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    public async Task<PagedResult<CourtListDto>> SearchCourtsAsync(CourtSearchParams searchParams)
    {
        try
        {
            var query = new StringBuilder("api/courts?");
            if (searchParams.CourtTypeId.HasValue)
                query.Append($"CourtTypeId={searchParams.CourtTypeId}&");
            if (!string.IsNullOrEmpty(searchParams.Status))
                query.Append($"Status={Uri.EscapeDataString(searchParams.Status)}&");
            if (searchParams.MinPrice.HasValue)
                query.Append($"MinPrice={searchParams.MinPrice}&");
            if (searchParams.MaxPrice.HasValue)
                query.Append($"MaxPrice={searchParams.MaxPrice}&");
            if (searchParams.Date.HasValue)
                query.Append($"Date={searchParams.Date.Value.ToString("yyyy-MM-dd")}&");
            if (searchParams.TimeSlotId.HasValue)
                query.Append($"TimeSlotId={searchParams.TimeSlotId}&");
            if (!string.IsNullOrEmpty(searchParams.SearchTerm))
                query.Append($"SearchTerm={Uri.EscapeDataString(searchParams.SearchTerm)}&");
            if (!string.IsNullOrEmpty(searchParams.SortBy))
                query.Append($"SortBy={Uri.EscapeDataString(searchParams.SortBy)}&");
            
            query.Append($"SortDescending={searchParams.SortDescending}&");
            query.Append($"PageNumber={searchParams.PageNumber}&");
            query.Append($"PageSize={searchParams.PageSize}");

            var response = await _httpClient.GetFromJsonAsync<PagedResult<CourtListDto>>(query.ToString(), _jsonOptions);
            if (response != null)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SearchCourts API");
        }

        return new PagedResult<CourtListDto>();
    }

    public async Task<CourtDetailDto?> GetCourtDetailAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CourtDetailDto>($"api/courts/{id}", _jsonOptions);
            if (response != null)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetCourtDetail API for ID {Id}", id);
        }
        return null;
    }

    public async Task<CourtAvailabilityDto?> GetCourtAvailabilityAsync(int id, DateTime date)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CourtAvailabilityDto>($"api/courts/{id}/availability?date={date:yyyy-MM-dd}");
            if (response != null)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetCourtAvailability API for ID {Id} and Date {Date}", id, date);
        }
        return null;
    }

    public async Task<PagedResult<ReviewDto>> GetCourtReviewsAsync(int courtId, int pageNumber, int pageSize)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<PagedResult<ReviewDto>>($"api/courts/{courtId}/reviews?pageNumber={pageNumber}&pageSize={pageSize}");
            if (response != null)
            {
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetCourtReviews API for court {CourtId}", courtId);
        }
        return new PagedResult<ReviewDto>();
    }

    public async Task<List<CourtTypeDto>> GetCourtTypesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<PagedResult<CourtListDto>>("api/courts?PageSize=50");
            if (response != null && response.Items != null)
            {
                var types = new List<CourtTypeDto>();
                var seenIds = new HashSet<int>();
                foreach (var court in response.Items)
                {
                    if (court.CourtTypeId > 0 && !seenIds.Contains(court.CourtTypeId))
                    {
                        seenIds.Add(court.CourtTypeId);
                        types.Add(new CourtTypeDto
                        {
                            CourtTypeId = court.CourtTypeId,
                            TypeName = court.CourtTypeName,
                            IsActive = true,
                            CourtCount = response.Items.Count(c => c.CourtTypeId == court.CourtTypeId)
                        });
                    }
                }
                return types;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetCourtTypes workaround");
        }
        return new List<CourtTypeDto>();
    }

    public async Task<List<ServiceDto>> GetServicesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<ServiceDto>>>("api/Services");
            if (response != null && (response.Success || response.Data != null))
            {
                return response.Data ?? new List<ServiceDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetServices API");
        }
        return new List<ServiceDto>();
    }

    public async Task<List<TimeSlotDto>> GetTimeSlotsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<TimeSlotDto>>>("api/time-slots");
            if (response != null && (response.Success || response.Data != null))
            {
                return response.Data ?? new List<TimeSlotDto>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetTimeSlots API");
        }
        return new List<TimeSlotDto>();
    }

    public async Task<(bool success, string message)> SubmitReviewAsync(int courtId, int bookingId, byte rating, string? comment, string? token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/courts/{courtId}/reviews");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            request.Content = JsonContent.Create(new
            {
                BookingId = bookingId,
                Rating = rating,
                Comment = comment
            });

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
                return (true, apiResponse?.Message ?? "Gửi đánh giá thành công!");
            }

            var errBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("SubmitReview API failed with status {Status}: {Body}", response.StatusCode, errBody);
            
            try
            {
                var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(errBody);
                var msg = errorObj?["message"]?.ToString();
                if (!string.IsNullOrEmpty(msg))
                {
                    return (false, msg);
                }
            }
            catch {}
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubmitReview API for court {CourtId}", courtId);
        }
        return (false, "Gửi đánh giá thất bại. Vui lòng kiểm tra lại thông tin đặt sân.");
    }

    public async Task<BookingResponseDto?> CreateBookingAsync(BookingRequestDto request, string? token)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "api/bookings");
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var backendRequest = new
            {
                CourtId = request.CourtId,
                SlotId = request.TimeSlotIds.Count > 0 ? request.TimeSlotIds[0] : 0,
                BookingDate = request.BookingDate.ToDateTime(TimeOnly.MinValue),
                ServiceIds = request.Services.Select(s => new { ServiceId = s.ServiceId, Quantity = s.Quantity }).ToList(),
                PromotionCode = request.PromotionCode,
                Note = request.Note
            };

            req.Content = JsonContent.Create(backendRequest);

            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BookingResponseDto>>();
                if (apiResponse != null && apiResponse.Success)
                {
                    return apiResponse.Data;
                }
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("CreateBooking API failed: {Body}", err);
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(err);
                    var msg = errorObj?["message"]?.ToString();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        throw new InvalidOperationException(msg);
                    }
                }
                catch (System.Text.Json.JsonException) { }
                catch (InvalidOperationException) { throw; }
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
        }
        return null;
    }

    public async Task<RecurringBookingResponseDto?> CreateRecurringBookingAsync(RecurringBookingRequestDto request, string? token)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "api/bookings/recurring");
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var backendRequest = new
            {
                CourtId = request.CourtId,
                SlotId = request.SlotId,
                StartDate = request.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = request.EndDate.ToDateTime(TimeOnly.MinValue),
                DaysOfWeek = request.DaysOfWeek,
                PromotionCode = request.PromotionCode,
                Note = request.Note
            };

            req.Content = JsonContent.Create(backendRequest);

            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<RecurringBookingResponseDto>>();
                if (apiResponse != null && apiResponse.Success)
                {
                    return apiResponse.Data;
                }
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("CreateRecurringBooking API failed: {Body}", err);
                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.Nodes.JsonNode>(err);
                    var msg = errorObj?["message"]?.ToString();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        throw new InvalidOperationException(msg);
                    }
                }
                catch (System.Text.Json.JsonException) { }
                catch (InvalidOperationException) { throw; }
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating recurring booking");
        }
        return null;
    }

    public async Task<PaymentResponseDto?> CreatePaymentLinkAsync(PaymentRequestDto request, string? token)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "api/payments/create-link");
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            req.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentResponseDto>>();
                if (apiResponse != null && apiResponse.Success)
                {
                    return apiResponse.Data;
                }
            }
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("CreatePaymentLink API failed: {Body}", err);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment link");
        }
        return null;
    }

    public async Task<BookingResponseDto?> GetBookingDetailAsync(int id, string? token)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, $"api/bookings/{id}");
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BookingResponseDto>>();
                if (apiResponse != null && apiResponse.Success)
                {
                    return apiResponse.Data;
                }
            }
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("GetBookingDetail API failed: {Body}", err);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking detail for {Id}", id);
        }
        return null;
    }

    // Promotions
    public async Task<PagedResult<PromotionDto>> GetPagedPromotionsAsync(string? keyword, bool? isActive, int pageNumber, int pageSize)
    {
        try
        {
            var url = $"api/promotions?PageNumber={pageNumber}&PageSize={pageSize}";
            if (!string.IsNullOrEmpty(keyword)) url += $"&Keyword={Uri.EscapeDataString(keyword)}";
            if (isActive.HasValue) url += $"&IsActive={isActive}";
            var req = CreateAuthRequest(HttpMethod.Get, url);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<PromotionDto>>>(_jsonOptions);
                if (body != null && body.Data != null) return body.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetPagedPromotionsAsync"); }
        return new PagedResult<PromotionDto>();
    }

    public async Task<PromotionDto?> CreatePromotionAsync(PromotionFormDto form)
    {
        try
        {
            var req = CreateAuthRequest(HttpMethod.Post, "api/promotions");
            req.Content = JsonContent.Create(form, null, _jsonOptions);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>(_jsonOptions);
                return body?.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error CreatePromotionAsync"); }
        return null;
    }

    public async Task<PromotionDto?> UpdatePromotionAsync(int id, PromotionFormDto form)
    {
        try
        {
            var req = CreateAuthRequest(HttpMethod.Put, $"api/promotions/{id}");
            req.Content = JsonContent.Create(form, null, _jsonOptions);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PromotionDto>>(_jsonOptions);
                return body?.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error UpdatePromotionAsync for id {Id}", id); }
        return null;
    }

    public async Task<bool> DeletePromotionAsync(int id)
    {
        try
        {
            var req = CreateAuthRequest(HttpMethod.Delete, $"api/promotions/{id}");
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "Error DeletePromotionAsync for id {Id}", id); }
        return false;
    }

    // Bookings (Admin & Customer)
    public async Task<PagedResult<BookingDetailDto>> GetPagedMyBookingsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int pageNumber, int pageSize, string? token)
    {
        try
        {
            var url = BuildFilterQuery("api/bookings/my", keyword, fromDate, toDate, status, pageNumber, pageSize);
            var req = CreateAuthRequest(HttpMethod.Get, url, token);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<BookingDetailDto>>>(_jsonOptions);
                if (body != null && body.Data != null) return body.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetPagedMyBookingsAsync"); }
        return new PagedResult<BookingDetailDto>();
    }

    public async Task<PagedResult<BookingDetailDto>> GetPagedAdminBookingsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, int? courtTypeId, string? status, int pageNumber, int pageSize)
    {
        try
        {
            var url = BuildFilterQuery("api/bookings/admin", keyword, fromDate, toDate, status, pageNumber, pageSize);
            if (courtTypeId.HasValue && courtTypeId.Value > 0) url += $"&CourtTypeId={courtTypeId.Value}";
            var req = CreateAuthRequest(HttpMethod.Get, url);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<BookingDetailDto>>>(_jsonOptions);
                if (body != null && body.Data != null) return body.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetPagedAdminBookingsAsync"); }
        return new PagedResult<BookingDetailDto>();
    }

    public async Task<bool> UpdateBookingStatusAsync(int id, string status, string? cancelReason)
    {
        try
        {
            var req = CreateAuthRequest(HttpMethod.Put, $"api/bookings/{id}/status");
            req.Content = JsonContent.Create(new { Status = status, CancelReason = cancelReason }, null, _jsonOptions);
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "Error UpdateBookingStatusAsync for id {Id}", id); }
        return false;
    }

    // Tournaments
    public async Task<PagedResult<TournamentDto>> GetPagedMyTournamentsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int pageNumber, int pageSize)
    {
        try
        {
            var url = BuildFilterQuery("api/bookings/tournament/my", keyword, fromDate, toDate, status, pageNumber, pageSize);
            var req = CreateAuthRequest(HttpMethod.Get, url);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TournamentDto>>>(_jsonOptions);
                if (body != null && body.Data != null) return body.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetPagedMyTournamentsAsync"); }
        return new PagedResult<TournamentDto>();
    }

    public async Task<PagedResult<TournamentDto>> GetPagedAdminTournamentsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int pageNumber, int pageSize)
    {
        try
        {
            var url = BuildFilterQuery("api/bookings/tournament/admin", keyword, fromDate, toDate, status, pageNumber, pageSize);
            var req = CreateAuthRequest(HttpMethod.Get, url);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TournamentDto>>>(_jsonOptions);
                if (body != null && body.Data != null) return body.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetPagedAdminTournamentsAsync"); }
        return new PagedResult<TournamentDto>();
    }

    public async Task<PagedResult<TournamentPublicDto>> GetPagedPublicTournamentsAsync(string? keyword, int pageNumber, int pageSize)
    {
        try
        {
            var url = $"api/bookings/tournament/public?PageNumber={pageNumber}&PageSize={pageSize}";
            if (!string.IsNullOrEmpty(keyword)) url += $"&Keyword={Uri.EscapeDataString(keyword)}";
            var res = await _httpClient.GetAsync(url);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TournamentPublicDto>>>(_jsonOptions);
                if (body != null && body.Data != null) return body.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetPagedPublicTournamentsAsync"); }
        return new PagedResult<TournamentPublicDto>();
    }

    public async Task<TournamentDto?> CreateTournamentAsync(CreateTournamentFormDto form)
    {
        var (data, _) = await CreateTournamentResultAsync(form);
        return data;
    }

    public async Task<(TournamentDto? Data, string? ErrorMessage)> CreateTournamentResultAsync(CreateTournamentFormDto form)
    {
        try
        {
            var req = CreateAuthRequest(HttpMethod.Post, "api/bookings/tournament");
            req.Content = JsonContent.Create(form, null, _jsonOptions);
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<TournamentDto>>(_jsonOptions);
                return (body?.Data, null);
            }
            var rawErr = await res.Content.ReadAsStringAsync();
            try
            {
                var errBody = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<object>>(rawErr, _jsonOptions);
                if (errBody != null && !string.IsNullOrWhiteSpace(errBody.Message)) return (null, errBody.Message);

                // Thử parse xem có phải ValidationProblemDetails không
                var problem = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(rawErr);
                if (problem.TryGetProperty("errors", out var errors) && errors.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var firstErr = errors.EnumerateObject().FirstOrDefault();
                    if (firstErr.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var msg = firstErr.Value.EnumerateArray().FirstOrDefault().GetString();
                        if (!string.IsNullOrEmpty(msg)) return (null, msg);
                    }
                }
            }
            catch { }
            
            // Hiển thị chi tiết lỗi để dễ debug
            string finalErr = $"Lỗi {res.StatusCode} ({(int)res.StatusCode}). ";
            if (!string.IsNullOrWhiteSpace(rawErr)) {
                finalErr += $"Chi tiết: {rawErr}";
            } else {
                finalErr += "Server không trả về chi tiết lỗi.";
            }
            return (null, finalErr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error CreateTournamentResultAsync");
            return (null, "Lỗi kết nối đến máy chủ.");
        }
    }

    public async Task<TournamentDto?> GetMyTournamentDetailAsync(int id)
    {
        try
        {
            var req = CreateAuthRequest(HttpMethod.Get, $"api/bookings/tournament/{id}");
            var res = await _httpClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadFromJsonAsync<ApiResponse<TournamentDto>>(_jsonOptions);
                return body?.Data;
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetMyTournamentDetailAsync for tournament {Id}", id); }
        return null;
    }

    public async Task<bool> UpdateTournamentStatusAsync(int id, string status, string? cancelReason)
    {
        try
        {
            var req = CreateAuthRequest(HttpMethod.Put, $"api/bookings/tournament/{id}/status");
            req.Content = JsonContent.Create(new { Status = status, CancelReason = cancelReason }, null, _jsonOptions);
            var res = await _httpClient.SendAsync(req);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex) { _logger.LogError(ex, "Error UpdateTournamentStatusAsync"); }
        return false;
    }

    public async Task<SePayQrCodeDto?> GetSePayQrCodeAsync(string bookingOrTournamentCode)
    {
        try
        {
            var res = await _httpClient.GetAsync($"api/SePay/qr-code/{Uri.EscapeDataString(bookingOrTournamentCode)}");
            if (res.IsSuccessStatusCode)
            {
                return await res.Content.ReadFromJsonAsync<SePayQrCodeDto>(_jsonOptions);
            }
        }
        catch (Exception ex) { _logger.LogError(ex, "Error GetSePayQrCodeAsync for {Code}", bookingOrTournamentCode); }
        return null;
    }

    private static HttpRequestMessage CreateAuthRequest(HttpMethod method, string url, string? token = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(token)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }

    private static string BuildFilterQuery(string baseUrl, string? kw, DateTime? from, DateTime? to, string? st, int page, int size)
    {
        var url = $"{baseUrl}?PageNumber={page}&PageSize={size}";
        if (!string.IsNullOrEmpty(kw)) url += $"&Keyword={Uri.EscapeDataString(kw)}";
        if (from.HasValue) url += $"&FromDate={from.Value:yyyy-MM-dd}";
        if (to.HasValue) url += $"&ToDate={to.Value:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(st)) url += $"&Status={Uri.EscapeDataString(st)}";
        return url;
    }

    // Auth Implementations
    public async Task<Models.Auth.AuthLoginResult> LoginAsync(Models.Auth.LoginRequest request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/auth/login", request, _jsonOptions);
            var body = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                var wrapper = JsonSerializer.Deserialize<ApiResponse<Models.Auth.AuthResponse>>(body, _jsonOptions);
                if (wrapper?.Data != null)
                    return Models.Auth.AuthLoginResult.Ok(wrapper.Data);
            }

            // Parse error
            string errMsg = "Email hoặc mật khẩu không đúng.";
            bool requiresVerify = false;
            try
            {
                var errWrapper = JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions);
                if (!string.IsNullOrEmpty(errWrapper?.Message)) errMsg = errWrapper.Message;
                if ((int)res.StatusCode == 403 && errMsg.Contains("xác thực", StringComparison.OrdinalIgnoreCase))
                    requiresVerify = true;
            }
            catch { }

            return Models.Auth.AuthLoginResult.Fail(errMsg, requiresVerify);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoginAsync");
            return Models.Auth.AuthLoginResult.Fail("Không kết nối được API Backend.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(Models.Auth.RegisterRequest request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/auth/register", request, _jsonOptions);
            var body = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                return (true, null);
            }
            try
            {
                var errWrapper = JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions);
                return (false, errWrapper?.Message ?? "Đăng ký thất bại.");
            }
            catch { }
            return (false, "Đăng ký thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RegisterAsync");
            return (false, "Không kết nối được API Backend.");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> VerifyEmailAsync(Models.Auth.VerifyEmailRequest request)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync("api/auth/verify-email", request, _jsonOptions);
            var body = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                return (true, null);
            }
            try
            {
                var errWrapper = JsonSerializer.Deserialize<ApiResponse<object>>(body, _jsonOptions);
                return (false, errWrapper?.Message ?? "Xác thực thất bại.");
            }
            catch { }
            return (false, "Xác thực thất bại.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VerifyEmailAsync");
            return (false, "Không kết nối được API Backend.");
        }
    }

    public async Task<List<PromotionDto>> GetActivePromotionsAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<PromotionDto>>>("api/promotions");
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            _logger.LogWarning("GetActivePromotions API returned unsuccessful status: {Message}", response?.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetActivePromotions API");
        }
        return new List<PromotionDto>();
    }

    public async Task<string> GetRawJsonAsync(string relativeUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(relativeUrl);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                _logger.LogWarning("GetRawJsonAsync from {Url} returned non-success status: {StatusCode}", relativeUrl, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetRawJsonAsync for url {Url}", relativeUrl);
        }
        return "{}";
    }
}
