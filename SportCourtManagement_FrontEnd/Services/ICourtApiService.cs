using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Reviews;

namespace SportCourtManagement_FrontEnd.Services;

public interface ICourtApiService
{
    Task<PagedResult<CourtListDto>> SearchCourtsAsync(CourtSearchParams searchParams);
    Task<CourtDetailDto?> GetCourtDetailAsync(int id);
    Task<CourtAvailabilityDto?> GetCourtAvailabilityAsync(int id, DateOnly date);
    Task<PagedResult<ReviewDto>> GetCourtReviewsAsync(int courtId, int pageNumber, int pageSize);
    Task<List<CourtTypeDto>> GetCourtTypesAsync();
    Task<bool> SubmitReviewAsync(int courtId, int bookingId, byte rating, string? comment, string? token);
}
