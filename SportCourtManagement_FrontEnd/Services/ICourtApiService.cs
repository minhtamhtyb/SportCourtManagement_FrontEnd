using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.Courts;
using SportCourtManagement_FrontEnd.Models.Reviews;
using SportCourtManagement_FrontEnd.Models.Bookings;
using SportCourtManagement_FrontEnd.Models.Promotions;
using SportCourtManagement_FrontEnd.Models.Tournaments;
using SportCourtManagement_FrontEnd.Models.Services;

namespace SportCourtManagement_FrontEnd.Services;

public interface ICourtApiService
{
    Task<PagedResult<CourtListDto>> SearchCourtsAsync(CourtSearchParams searchParams);
    Task<CourtDetailDto?> GetCourtDetailAsync(int id);
    Task<CourtAvailabilityDto?> GetCourtAvailabilityAsync(int id, DateOnly date);
    Task<PagedResult<ReviewDto>> GetCourtReviewsAsync(int courtId, int pageNumber, int pageSize);
    Task<List<CourtTypeDto>> GetCourtTypesAsync();
    Task<List<ServiceDto>> GetServicesAsync();
    Task<List<TimeSlotDto>> GetTimeSlotsAsync();
    Task<bool> SubmitReviewAsync(int courtId, int bookingId, byte rating, string? comment, string? token);
    Task<BookingResponseDto?> CreateBookingAsync(BookingRequestDto request, string? token);
    Task<PaymentResponseDto?> CreatePaymentLinkAsync(PaymentRequestDto request, string? token);
    Task<BookingResponseDto?> GetBookingDetailAsync(int id, string? token);

    // Promotions
    Task<PagedResult<PromotionDto>> GetPagedPromotionsAsync(string? keyword, bool? isActive, int pageNumber, int pageSize, string? token);
    Task<PromotionDto?> CreatePromotionAsync(PromotionFormDto form, string? token);
    Task<PromotionDto?> UpdatePromotionAsync(int id, PromotionFormDto form, string? token);
    Task<bool> DeletePromotionAsync(int id, string? token);

    // Bookings (Admin & Customer)
    Task<PagedResult<BookingDetailDto>> GetPagedMyBookingsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int pageNumber, int pageSize, string? token);
    Task<PagedResult<BookingDetailDto>> GetPagedAdminBookingsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, int? courtTypeId, string? status, int pageNumber, int pageSize, string? token);
    Task<bool> UpdateBookingStatusAsync(int id, string status, string? cancelReason, string? token);

    // Tournaments
    Task<PagedResult<TournamentDto>> GetPagedMyTournamentsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int pageNumber, int pageSize, string? token);
    Task<PagedResult<TournamentDto>> GetPagedAdminTournamentsAsync(string? keyword, DateTime? fromDate, DateTime? toDate, string? status, int pageNumber, int pageSize, string? token);
    Task<PagedResult<TournamentPublicDto>> GetPagedPublicTournamentsAsync(string? keyword, int pageNumber, int pageSize);
    Task<TournamentDto?> CreateTournamentAsync(CreateTournamentFormDto form, string? token);
    Task<(TournamentDto? Data, string? ErrorMessage)> CreateTournamentResultAsync(CreateTournamentFormDto form, string? token);
    Task<TournamentDto?> GetMyTournamentDetailAsync(int id, string? token);
    Task<bool> UpdateTournamentStatusAsync(int id, string status, string? cancelReason, string? token);
    Task<SePayQrCodeDto?> GetSePayQrCodeAsync(string bookingOrTournamentCode);

    // Auth
    Task<Models.Auth.AuthLoginResult> LoginAsync(Models.Auth.LoginRequest request);
    Task<(bool Success, string? ErrorMessage)> RegisterAsync(Models.Auth.RegisterRequest request);
    Task<(bool Success, string? ErrorMessage)> VerifyEmailAsync(Models.Auth.VerifyEmailRequest request);
}


