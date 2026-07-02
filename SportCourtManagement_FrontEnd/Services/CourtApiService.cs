using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Reviews;

namespace SportCourtManagement_FrontEnd.Services;

public class CourtApiService : ICourtApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CourtApiService> _logger;

    public CourtApiService(HttpClient httpClient, ILogger<CourtApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> StatusCodeAsync()
    {
        var query = new StringBuilder("api/courts");
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<CourtListDto>>>(query.ToString());

        return $"Status code: {response.StatusCode}. Body: {response.Data}";
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

            var response = await _httpClient.GetFromJsonAsync<PagedResult<CourtListDto>>(query.ToString());
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
            var response = await _httpClient.GetFromJsonAsync<CourtDetailDto>($"api/courts/{id}");
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
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<ReviewDto>>>($"api/courts/{courtId}/reviews?pageNumber={pageNumber}&pageSize={pageSize}");
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            _logger.LogWarning("GetCourtReviews API returned unsuccessful: {Message}", response?.Message);
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
}
