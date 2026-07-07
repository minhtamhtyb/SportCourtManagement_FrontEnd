using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class CourtsController(ICourtService courtService) : Controller
{
    public async Task<IActionResult> Create(int complexId)
    {
        var complex = await courtService.GetComplexByIdAsync(complexId);
        if (complex == null) return NotFound();

        var vm = await BuildFormViewModel(complexId, complex.ComplexName);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int complexId, CourtFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateFormOptions(model, complexId);
            return View("Create", model);
        }

        if (model.CloseTime.CompareTo(model.OpenTime) <= 0)
        {
            ModelState.AddModelError(nameof(model.CloseTime), "Giờ đóng cửa phải sau giờ mở cửa.");
            await PopulateFormOptions(model, complexId);
            return View("Create", model);
        }

        await courtService.CreateCourtAsync(MapToDto(model, complexId));
        TempData["Success"] = "Thêm sân mới thành công!";
        return RedirectToAction("Details", "Complexes", new { id = complexId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var court = await courtService.GetCourtByIdAsync(id);
        if (court == null) return NotFound();

        var vm = await BuildFormViewModel(court.ComplexId ?? 0, court.ComplexName);
        vm.CourtId = court.CourtId;
        vm.CourtName = court.CourtName;
        vm.CourtCode = court.CourtCode;
        vm.CourtTypeId = court.CourtTypeId;
        vm.Description = court.Description;
        vm.Location = court.Location;
        vm.Capacity = court.Capacity;
        vm.Surface = court.Surface;
        vm.ImageUrl = court.ImageUrl;
        vm.Status = court.Status;
        vm.OpenTime = court.OpenTime;
        vm.CloseTime = court.CloseTime;
        vm.PricePerHour = court.PricePerHour;
        vm.CourtSize = court.CourtSize;
        return View("Create", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourtFormViewModel model)
    {
        var court = await courtService.GetCourtByIdAsync(id);
        if (court == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateFormOptions(model, model.ComplexId);
            return View("Create", model);
        }

        await courtService.UpdateCourtAsync(id, MapToDto(model, model.ComplexId));
        TempData["Success"] = "Cập nhật sân thành công!";
        return RedirectToAction("Details", "Complexes", new { id = model.ComplexId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, int complexId)
    {
        await courtService.UpdateCourtStatusAsync(id, status);
        TempData["Success"] = "Cập nhật trạng thái sân thành công!";
        return RedirectToAction("Details", "Complexes", new { id = complexId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int complexId)
    {
        await courtService.DeleteCourtAsync(id);
        TempData["Success"] = "Xóa sân thành công!";
        return RedirectToAction("Details", "Complexes", new { id = complexId });
    }

    private async Task<CourtFormViewModel> BuildFormViewModel(int complexId, string? complexName)
    {
        var vm = new CourtFormViewModel { ComplexId = complexId, ComplexName = complexName };
        await PopulateFormOptions(vm, complexId);
        return vm;
    }

    private async Task PopulateFormOptions(CourtFormViewModel vm, int complexId)
    {
        vm.ComplexId = complexId;
        vm.CourtTypes = await courtService.GetCourtTypesAsync();
    }

    private static CourtDto MapToDto(CourtFormViewModel model, int complexId) => new()
    {
        CourtName = model.CourtName,
        CourtCode = model.CourtCode.ToUpperInvariant(),
        CourtTypeId = model.CourtTypeId,
        ComplexId = complexId,
        Description = model.Description,
        Location = model.Location,
        Capacity = model.Capacity,
        Surface = model.Surface,
        ImageUrl = model.ImageUrl,
        Status = model.Status,
        OpenTime = model.OpenTime,
        CloseTime = model.CloseTime,
        PricePerHour = model.PricePerHour,
        CourtSize = model.CourtSize
    };
}
