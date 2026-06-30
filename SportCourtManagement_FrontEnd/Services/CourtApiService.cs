using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SportCourtManagement_FrontEnd.Models.Api;
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

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<CourtListDto>>>(query.ToString());
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            
            _logger.LogWarning("SearchCourts API returned unsuccessful status: {Message}", response?.Message);
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
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<CourtDetailDto>>($"api/courts/{id}");
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            _logger.LogWarning("GetCourtDetail API returned unsuccessful status: {Message}", response?.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetCourtDetail API for ID {Id}", id);
        }
        return null;
    }

    public async Task<CourtAvailabilityDto?> GetCourtAvailabilityAsync(int id, DateOnly date)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<CourtAvailabilityDto>>($"api/courts/{id}/availability?date={date:yyyy-MM-dd}");
            if (response != null && response.Success)
            {
                return response.Data;
            }
            _logger.LogWarning("GetCourtAvailability API returned unsuccessful status: {Message}", response?.Message);
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
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<CourtTypeDto>>>("api/court-types");
            if (response != null && response.Success && response.Data != null)
            {
                return response.Data;
            }
            _logger.LogWarning("GetCourtTypes API returned unsuccessful: {Message}", response?.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling GetCourtTypes API");
        }
        return new List<CourtTypeDto>();
    }

    public async Task<bool> SubmitReviewAsync(int courtId, int bookingId, byte rating, string? comment, string? token)
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
                return apiResponse?.Success ?? false;
            }

            var errBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("SubmitReview API failed with status {Status}: {Body}", response.StatusCode, errBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubmitReview API for court {CourtId}", courtId);
        }
        return false;
    }
}
