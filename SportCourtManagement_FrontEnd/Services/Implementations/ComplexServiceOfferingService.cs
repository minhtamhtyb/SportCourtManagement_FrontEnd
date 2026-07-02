using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Services.Api;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class ApiComplexServiceOfferingService(ApiClient api) : IComplexServiceOfferingService
{
    public async Task<List<ComplexCourtTypeServiceDto>> GetByComplexAsync(int complexId)
    {
        var data = await api.GetDataAsync<List<ComplexCourtTypeServiceDto>>($"api/complexes/{complexId}/services");
        return data ?? [];
    }

    public async Task<List<ComplexCourtTypeServiceDto>> GetByComplexAndCourtTypeAsync(int complexId, int courtTypeId)
    {
        var data = await api.GetDataAsync<List<ComplexCourtTypeServiceDto>>(
            $"api/complexes/{complexId}/court-types/{courtTypeId}/services");
        return data ?? [];
    }

    public async Task<ComplexCourtTypeServiceDto> CreateAsync(int complexId, int courtTypeId, ComplexCourtTypeServiceDto dto)
    {
        var payload = new
        {
            serviceId = dto.ServiceId,
            price = dto.Price,
            stockQty = dto.StockQty,
            serviceMode = dto.ServiceMode == "Included" ? 0 : 1,
            isActive = dto.IsActive
        };
        return await api.PostDataAsync<ComplexCourtTypeServiceDto>(
            $"api/complexes/{complexId}/court-types/{courtTypeId}/services", payload)
            ?? throw new InvalidOperationException("Thêm dịch vụ thất bại.");
    }

    public async Task UpdateAsync(int offeringId, ComplexCourtTypeServiceDto dto)
    {
        var payload = new
        {
            price = dto.Price,
            stockQty = dto.StockQty,
            serviceMode = dto.ServiceMode == "Included" ? 0 : 1,
            isActive = dto.IsActive
        };
        await api.PutDataAsync<ComplexCourtTypeServiceDto>($"api/complex-service-offerings/{offeringId}", payload);
    }

    public Task DeleteAsync(int offeringId) =>
        api.DeleteAsync($"api/complex-service-offerings/{offeringId}");
}

public class MockComplexServiceOfferingService(MockDataStore store) : IComplexServiceOfferingService
{
    public Task<List<ComplexCourtTypeServiceDto>> GetByComplexAsync(int complexId) =>
        Task.FromResult(store.ServiceOfferings.Where(o => o.ComplexId == complexId).ToList());

    public Task<List<ComplexCourtTypeServiceDto>> GetByComplexAndCourtTypeAsync(int complexId, int courtTypeId) =>
        Task.FromResult(store.ServiceOfferings
            .Where(o => o.ComplexId == complexId && o.CourtTypeId == courtTypeId)
            .ToList());

    public Task<ComplexCourtTypeServiceDto> CreateAsync(int complexId, int courtTypeId, ComplexCourtTypeServiceDto dto)
    {
        var catalog = store.Services.FirstOrDefault(s => s.ServiceId == dto.ServiceId)
            ?? throw new InvalidOperationException("Không tìm thấy dịch vụ trong danh mục.");

        if (store.ServiceOfferings.Any(o => o.ComplexId == complexId && o.CourtTypeId == courtTypeId && o.ServiceId == dto.ServiceId))
            throw new InvalidOperationException("Dịch vụ này đã được gán cho loại sân.");

        var courtType = store.CourtTypes.FirstOrDefault(t => t.CourtTypeId == courtTypeId);
        var offering = new ComplexCourtTypeServiceDto
        {
            OfferingId = store.NextOfferingId(),
            ComplexId = complexId,
            CourtTypeId = courtTypeId,
            CourtTypeName = courtType?.TypeName ?? "",
            ServiceId = dto.ServiceId,
            ServiceName = catalog.ServiceName,
            Category = catalog.Category,
            Unit = catalog.Unit ?? "",
            Price = dto.ServiceMode == "Included" ? 0 : dto.Price,
            StockQty = dto.StockQty,
            ServiceMode = dto.ServiceMode,
            IsActive = dto.IsActive
        };
        store.ServiceOfferings.Add(offering);
        return Task.FromResult(offering);
    }

    public Task UpdateAsync(int offeringId, ComplexCourtTypeServiceDto dto)
    {
        var existing = store.ServiceOfferings.FirstOrDefault(o => o.OfferingId == offeringId)
            ?? throw new InvalidOperationException("Không tìm thấy cấu hình dịch vụ.");
        existing.Price = dto.ServiceMode == "Included" ? 0 : dto.Price;
        existing.StockQty = dto.StockQty;
        existing.ServiceMode = dto.ServiceMode;
        existing.IsActive = dto.IsActive;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int offeringId)
    {
        store.ServiceOfferings.RemoveAll(o => o.OfferingId == offeringId);
        return Task.CompletedTask;
    }
}
