using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ServicesController(IServiceCatalogService serviceCatalog) : Controller
{
    public async Task<IActionResult> Index(string? category, string? search)
    {
        var vm = new ServiceListViewModel
        {
            Services = await serviceCatalog.GetServicesAsync(category, search),
            Category = category,
            Search = search
        };
        return View(vm);
    }

    public IActionResult Create() => View("Form", new ServiceFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        await serviceCatalog.CreateServiceAsync(MapToDto(model));
        TempData["Success"] = "Thêm dịch vụ thành công!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var svc = await serviceCatalog.GetServiceByIdAsync(id);
        if (svc == null) return NotFound();

        return View("Form", new ServiceFormViewModel
        {
            ServiceId = svc.ServiceId,
            ServiceName = svc.ServiceName,
            Category = svc.Category,
            Price = svc.Price,
            Unit = svc.Unit,
            Description = svc.Description,
            StockQty = svc.StockQty,
            IsActive = svc.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        await serviceCatalog.UpdateServiceAsync(id, MapToDto(model));
        TempData["Success"] = "Cập nhật dịch vụ thành công!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var svc = await serviceCatalog.GetServiceByIdAsync(id);
        if (svc == null) return NotFound();
        return View(svc);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await serviceCatalog.DeleteServiceAsync(id);
        TempData["Success"] = "Vô hiệu hóa dịch vụ thành công!";
        return RedirectToAction(nameof(Index));
    }

    private static ServiceDto MapToDto(ServiceFormViewModel model) => new()
    {
        ServiceName = model.ServiceName,
        Category = model.Category,
        Price = model.Price,
        Unit = model.Unit,
        Description = model.Description,
        StockQty = model.StockQty,
        IsActive = model.IsActive
    };
}
