using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ComplexesController(ICourtService courtService) : Controller
{
    public async Task<IActionResult> Index(string? search, int? courtTypeId, int page = 1)
    {
        var result = await courtService.GetComplexesAsync(search, courtTypeId, page, 8);
        var vm = new ComplexListViewModel
        {
            Complexes = result.Items,
            Stats = await courtService.GetStatsAsync(),
            CourtTypes = await courtService.GetCourtTypesAsync(),
            Search = search,
            CourtTypeId = courtTypeId,
            Page = page,
            PageSize = 8,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        };
        return View(vm);
    }

    public async Task<IActionResult> Create()
    {
        return View("Form", await BuildFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplexFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormOptions(model);
            return View("Form", model);
        }

        await courtService.CreateComplexAsync(MapToDto(model));
        TempData["Success"] = "Thêm tổ hợp sân thành công!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var complex = await courtService.GetComplexByIdAsync(id);
        if (complex == null) return NotFound();

        var vm = await BuildFormViewModel();
        vm.ComplexId = complex.ComplexId;
        vm.ComplexName = complex.ComplexName;
        vm.Address = complex.Address;
        vm.Phone = complex.Phone;
        vm.ManagerId = complex.ManagerId;
        vm.ManagerName = complex.ManagerName;
        vm.Description = complex.Description;
        vm.ImageUrl = complex.ImageUrl;
        vm.CourtTypeIds = complex.CourtTypeIds;
        return View("Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ComplexFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormOptions(model);
            return View("Form", model);
        }

        await courtService.UpdateComplexAsync(id, MapToDto(model));
        TempData["Success"] = "Cập nhật tổ hợp sân thành công!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id, string? search, string? status)
    {
        var complex = await courtService.GetComplexByIdAsync(id);
        if (complex == null) return NotFound();

        var vm = new ComplexDetailsViewModel
        {
            Complex = complex,
            Manager = complex.ManagerId.HasValue
                ? await courtService.GetManagerByIdAsync(complex.ManagerId.Value)
                : null,
            Courts = await courtService.GetCourtsByComplexAsync(id, search, status),
            CourtTypes = await courtService.GetCourtTypesAsync(),
            Search = search,
            StatusFilter = status
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await courtService.DeleteComplexAsync(id);
            TempData["Success"] = "Xóa tổ hợp sân thành công!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<ComplexFormViewModel> BuildFormViewModel()
    {
        var vm = new ComplexFormViewModel();
        await PopulateFormOptions(vm);
        return vm;
    }

    private async Task PopulateFormOptions(ComplexFormViewModel vm)
    {
        vm.CourtTypeOptions = await courtService.GetCourtTypesAsync();
        vm.Managers = await courtService.GetManagersAsync();
    }

    private static CourtComplexDto MapToDto(ComplexFormViewModel model) => new()
    {
        ComplexName = model.ComplexName,
        Address = model.Address,
        Phone = model.Phone,
        ManagerId = model.ManagerId,
        Description = model.Description,
        ImageUrl = model.ImageUrl,
        CourtTypeIds = model.CourtTypeIds
    };
}
