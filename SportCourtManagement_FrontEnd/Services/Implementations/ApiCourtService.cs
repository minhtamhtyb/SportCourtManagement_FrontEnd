using Microsoft.AspNetCore.Http;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Services.Api;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class ApiCourtService(ApiClient api) : ICourtService
{
    public Task<ComplexStatsDto> GetStatsAsync()
    {
        var stats = api.GetDataAsync<ComplexStatsDto>("api/complexes/stats");
        return stats.ContinueWith(t => t.Result ?? new ComplexStatsDto());
    }

    public async Task<PagedResult<CourtComplexDto>> GetComplexesAsync(
        string? search, int? courtTypeId, int page, int pageSize)
    {
        var query = $"api/complexes?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";
        if (courtTypeId.HasValue)
            query += $"&courtTypeId={courtTypeId.Value}";

        var result = await api.GetDataAsync<PagedComplexResult>(query)
            ?? new PagedComplexResult();

        return new PagedResult<CourtComplexDto>
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            PageNumber = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasNextPage = result.Page < result.TotalPages,
            HasPreviousPage = result.Page > 1
        };
    }

    public Task<CourtComplexDto?> GetComplexByIdAsync(int id) =>
        api.GetDataAsync<CourtComplexDto>($"api/complexes/{id}");

    public async Task<CourtComplexDto> CreateComplexAsync(CourtComplexDto dto)
    {
        var payload = new
        {
            dto.ComplexName,
            dto.Address,
            dto.ManagerId,
            dto.Description,
            dto.ImageUrl
        };
        return await api.PostDataAsync<CourtComplexDto>("api/complexes", payload)
            ?? throw new InvalidOperationException("Tạo tổ hợp sân thất bại.");
    }

    public async Task UpdateComplexAsync(int id, CourtComplexDto dto)
    {
        var payload = new
        {
            dto.ComplexName,
            dto.Address,
            dto.ManagerId,
            dto.Description,
            dto.ImageUrl
        };
        await api.PutDataAsync<CourtComplexDto>($"api/complexes/{id}", payload);
    }

    public Task DeleteComplexAsync(int id) =>
        api.DeleteAsync($"api/complexes/{id}");

    public async Task<string> UploadComplexImageAsync(IFormFile file)
    {
        var result = await api.PostMultipartDataAsync<ImageUploadResultApi>("api/complexes/upload-image", file);
        if (string.IsNullOrWhiteSpace(result?.Url))
            throw new InvalidOperationException("Upload ảnh thất bại — không nhận được URL.");
        return result.Url;
    }

    public async Task<List<CourtTypeDto>> GetCourtTypesAsync()
    {
        var types = await api.GetDataAsync<List<CourtTypeDto>>("api/court-types");
        return types ?? [];
    }

    public async Task<List<UserDto>> GetManagersAsync()
    {
        // Gọi endpoint mới /api/users/managers trả về List<UserSummaryApi> thẳng (không paged)
        var users = await api.GetDataAsync<List<UserSummaryApi>>("api/users/managers");
        return users?.Select(MapUser).ToList() ?? [];
    }

    public async Task<UserDto?> GetManagerByIdAsync(int id)
    {
        var user = await api.GetDataAsync<UserSummaryApi>($"api/users/{id}");
        return user is null ? null : MapUser(user);
    }

    public async Task<List<CourtDto>> GetCourtsByComplexAsync(int complexId, string? search, string? status)
    {
        var query = $"api/courts?complexId={complexId}";
        if (!string.IsNullOrWhiteSpace(status))
            query += $"&status={Uri.EscapeDataString(status)}";

        var courts = await api.GetDataAsync<List<ApiCourtDto>>(query) ?? [];
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            courts = courts.Where(c =>
                c.CourtName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.CourtCode.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return courts.Select(MapCourt).ToList();
    }

    public async Task<CourtDto?> GetCourtByIdAsync(int id)
    {
        var court = await api.GetDataAsync<ApiCourtDto>($"api/courts/{id}");
        return court is null ? null : MapCourt(court);
    }

    public async Task<CourtDto> CreateCourtAsync(CourtDto dto)
    {
        var (created, error, _) = await api.PostForResultAsync<ApiCourtDto>("api/courts", ToPayload(dto));
        if (error != null)
            throw new InvalidOperationException(error);
        return created is null ? dto : MapCourt(created);
    }

    public async Task UpdateCourtAsync(int id, CourtDto dto)
    {
        await api.PutDataAsync<object>($"api/courts/{id}", ToPayload(dto));
    }

    public async Task UpdateCourtStatusAsync(int id, string status)
    {
        var court = await GetCourtByIdAsync(id)
            ?? throw new InvalidOperationException("Không tìm thấy sân.");
        court.Status = status;
        await UpdateCourtAsync(id, court);
    }

    public Task DeleteCourtAsync(int id) =>
        api.DeleteAsync($"api/courts/{id}");

    private static object ToPayload(CourtDto dto) => new
    {
        dto.CourtName,
        dto.CourtCode,
        dto.CourtTypeId,
        dto.ComplexId,
        dto.Description,
        dto.Location,
        dto.Capacity,
        dto.Surface,
        dto.ImageUrl,
        dto.Status,
        dto.OpenTime,
        dto.CloseTime,
        dto.PricePerHour,
        dto.CourtSize,
        ImageUrls = dto.ImageUrls ?? new List<string>(),
        Pricings = dto.Pricings?.Select(p => new
        {
            p.SlotId,
            p.SlotName,
            p.StartTime,
            p.EndTime,
            p.Price
        }).ToList()
    };

    private static CourtDto MapCourt(ApiCourtDto c) => new()
    {
        CourtId = c.CourtId,
        CourtName = c.CourtName,
        CourtCode = c.CourtCode,
        CourtTypeId = c.CourtTypeId,
        CourtTypeName = c.CourtTypeName,
        ComplexId = c.ComplexId,
        ComplexName = c.ComplexName,
        Description = c.Description ?? "",
        Location = c.Location ?? "",
        Capacity = c.Capacity ?? 4,
        Surface = c.Surface,
        ImageUrl = c.ImageUrl ?? "",
        ImageUrls = c.ImageUrls ?? [],
        Status = c.Status,
        OpenTime = c.OpenTime,
        CloseTime = c.CloseTime,
        PricePerHour = c.PricePerHour,
        CourtSize = c.CourtSize,
        CreatedAt = c.CreatedAt,
        Pricings = c.Pricings
    };

    private static UserDto MapUser(UserSummaryApi u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Phone = u.Phone,
        AvatarUrl = u.AvatarUrl,
        Role = u.Role,
        IsActive = u.IsActive
    };

    private class PagedUsersApi
    {
        public List<UserSummaryApi> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    private class UserSummaryApi
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
    }

    private class ImageUploadResultApi
    {
        public string Url { get; set; } = "";
    }
}
