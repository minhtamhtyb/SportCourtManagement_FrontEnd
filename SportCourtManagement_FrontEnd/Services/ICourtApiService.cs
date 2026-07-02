using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Reviews;
using SportCourtManagement_FrontEnd.Models.Bookings;

namespace SportCourtManagement_FrontEnd.Services;

public interface ICourtApiService
{

    Task<string> StatusCodeAsync();
    Task<PagedResult<CourtListDto>> SearchCourtsAsync(CourtSearchParams searchParams);
    Task<CourtDetailDto?> GetCourtDetailAsync(int id);
    Task<CourtAvailabilityDto?> GetCourtAvailabilityAsync(int id, DateTime date);
    Task<PagedResult<ReviewDto>> GetCourtReviewsAsync(int courtId, int pageNumber, int pageSize);
    Task<List<CourtTypeDto>> GetCourtTypesAsync();
    Task<(bool success, string message)> SubmitReviewAsync(int courtId, int bookingId, byte rating, string? comment, string? token);
    Task<BookingResponseDto?> CreateBookingAsync(BookingRequestDto request, string? token);
    Task<RecurringBookingResponseDto?> CreateRecurringBookingAsync(RecurringBookingRequestDto request, string? token);
    Task<PaymentResponseDto?> CreatePaymentLinkAsync(PaymentRequestDto request, string? token);
    Task<BookingResponseDto?> GetBookingDetailAsync(int id, string? token);
    Task<List<PromotionDto>> GetActivePromotionsAsync();
}

