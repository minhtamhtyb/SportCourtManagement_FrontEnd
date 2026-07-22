using Microsoft.AspNetCore.Http;
using SportCourtManagement_FrontEnd.Models.Api;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class MockCourtService(MockDataStore store) : ICourtService
{
    public Task<ComplexStatsDto> GetStatsAsync() =>
        Task.FromResult(store.GetComplexStats());

    public Task<PagedResult<CourtComplexDto>> GetComplexesAsync(string? search, int? courtTypeId, int page, int pageSize)
    {
        var query = store.Complexes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.ComplexName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                c.Address.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (c.ManagerName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (courtTypeId.HasValue)
            query = query.Where(c => c.CourtTypeIds.Contains(courtTypeId.Value));

        var list = query.OrderBy(c => c.ComplexName).ToList();
        var total = list.Count;
        var items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Task.FromResult(new PagedResult<CourtComplexDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        });
    }

    public Task<CourtComplexDto?> GetComplexByIdAsync(int id) =>
        Task.FromResult(store.Complexes.FirstOrDefault(c => c.ComplexId == id));

    public Task<CourtComplexDto> CreateComplexAsync(CourtComplexDto dto)
    {
        dto.ComplexId = store.NextComplexId();
        dto.CreatedAt = DateTime.UtcNow;
        if (dto.ManagerId.HasValue)
            dto.ManagerName = store.Users.FirstOrDefault(u => u.UserId == dto.ManagerId)?.FullName;
        store.Complexes.Add(dto);
        store.RecalculateComplexStats();
        return Task.FromResult(dto);
    }

    public Task UpdateComplexAsync(int id, CourtComplexDto dto)
    {
        var existing = store.Complexes.FirstOrDefault(c => c.ComplexId == id)
            ?? throw new InvalidOperationException("Không tìm thấy tổ hợp sân.");
        existing.ComplexName = dto.ComplexName;
        existing.Address = dto.Address;
        existing.Phone = dto.Phone;
        existing.ManagerId = dto.ManagerId;
        existing.ManagerName = dto.ManagerId.HasValue
            ? store.Users.FirstOrDefault(u => u.UserId == dto.ManagerId)?.FullName
            : null;
        existing.Description = dto.Description;
        existing.ImageUrl = dto.ImageUrl;
        existing.CourtTypeIds = dto.CourtTypeIds;
        return Task.CompletedTask;
    }

    public Task DeleteComplexAsync(int id)
    {
        var complex = store.Complexes.FirstOrDefault(c => c.ComplexId == id)
            ?? throw new InvalidOperationException("Không tìm thấy tổ hợp sân.");
        if (store.Courts.Any(c => c.ComplexId == id))
            throw new InvalidOperationException("Vui lòng xóa hết sân trước khi xóa tổ hợp.");
        store.Complexes.Remove(complex);
        store.RecalculateComplexStats();
        return Task.CompletedTask;
    }

    public Task<List<CourtTypeDto>> GetCourtTypesAsync() =>
        Task.FromResult(store.CourtTypes.Where(t => t.IsActive).ToList());

    public Task<List<UserDto>> GetManagersAsync() =>
        Task.FromResult(store.Users.Where(u => (u.Role == "Staff" || u.Role == "Manager") && u.IsActive).ToList());

    public Task<string> UploadComplexImageAsync(IFormFile file) =>
        Task.FromResult($"https://picsum.photos/seed/{Guid.NewGuid():N}/800/400");

    public Task<UserDto?> GetManagerByIdAsync(int id) =>
        Task.FromResult(store.Users.FirstOrDefault(u => u.UserId == id));

    public Task<List<CourtDto>> GetCourtsByComplexAsync(int complexId, string? search, string? status)
    {
        var query = store.Courts.Where(c => c.ComplexId == complexId).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(c => c.CourtName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                     c.CourtCode.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);
        return Task.FromResult(query.OrderBy(c => c.CourtName).ToList());
    }

    public Task<CourtDto?> GetCourtByIdAsync(int id) =>
        Task.FromResult(store.Courts.FirstOrDefault(c => c.CourtId == id));

    public Task<CourtDto> CreateCourtAsync(CourtDto dto)
    {
        dto.CourtId = store.NextCourtId();
        dto.CreatedAt = DateTime.UtcNow;
        dto.CourtTypeName = store.CourtTypes.FirstOrDefault(t => t.CourtTypeId == dto.CourtTypeId)?.TypeName;
        dto.ComplexName = store.Complexes.FirstOrDefault(c => c.ComplexId == dto.ComplexId)?.ComplexName;
        store.Courts.Add(dto);
        store.RecalculateComplexStats();
        return Task.FromResult(dto);
    }

    public Task UpdateCourtAsync(int id, CourtDto dto)
    {
        var existing = store.Courts.FirstOrDefault(c => c.CourtId == id)
            ?? throw new InvalidOperationException("Không tìm thấy sân.");
        existing.CourtName = dto.CourtName;
        existing.CourtCode = dto.CourtCode;
        existing.CourtTypeId = dto.CourtTypeId;
        existing.CourtTypeName = store.CourtTypes.FirstOrDefault(t => t.CourtTypeId == dto.CourtTypeId)?.TypeName;
        existing.Description = dto.Description;
        existing.Location = dto.Location;
        existing.Capacity = dto.Capacity;
        existing.Surface = dto.Surface;
        existing.ImageUrl = dto.ImageUrl;
        existing.Status = dto.Status;
        existing.OpenTime = dto.OpenTime;
        existing.CloseTime = dto.CloseTime;
        existing.PricePerHour = dto.PricePerHour;
        existing.CourtSize = dto.CourtSize;
        store.RecalculateComplexStats();
        return Task.CompletedTask;
    }

    public Task UpdateCourtStatusAsync(int id, string status)
    {
        var court = store.Courts.FirstOrDefault(c => c.CourtId == id)
            ?? throw new InvalidOperationException("Không tìm thấy sân.");
        court.Status = status;
        store.RecalculateComplexStats();
        return Task.CompletedTask;
    }

    public Task DeleteCourtAsync(int id)
    {
        var court = store.Courts.FirstOrDefault(c => c.CourtId == id)
            ?? throw new InvalidOperationException("Không tìm thấy sân.");
        store.Courts.Remove(court);
        store.RecalculateComplexStats();
        return Task.CompletedTask;
    }
}
