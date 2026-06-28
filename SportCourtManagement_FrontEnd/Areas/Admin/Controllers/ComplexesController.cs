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
<<<<<<< HEAD
        var vm = await BuildFormViewModel();
        if (IsAjaxRequest()) return PartialView("_ComplexFormModal", vm);
        return View("Form", vm);
=======
        return View("Form", await BuildFormViewModel());
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplexFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
<<<<<<< HEAD
            if (IsAjaxRequest()) return JsonValidationErrors();
=======
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
            await PopulateFormOptions(model);
            return View("Form", model);
        }

<<<<<<< HEAD
        try
        {
            await ApplyImageUploadAsync(model);
            await courtService.CreateComplexAsync(MapToDto(model));
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Thêm tổ hợp sân thành công!" });
            TempData["Success"] = "Thêm tổ hợp sân thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
            {
                var status = ex.Message.Contains("401", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("xác thực", StringComparison.OrdinalIgnoreCase)
                    ? 401 : 400;
                return StatusCode(status, new { success = false, message = ex.Message });
            }
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateFormOptions(model);
            return View("Form", model);
        }
=======
        await courtService.CreateComplexAsync(MapToDto(model));
        TempData["Success"] = "Thêm tổ hợp sân thành công!";
        return RedirectToAction(nameof(Index));
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
    }

    public async Task<IActionResult> Edit(int id)
    {
<<<<<<< HEAD
        var vm = await BuildFormViewModelForEdit(id);
        if (vm == null) return NotFound();
        if (IsAjaxRequest()) return PartialView("_ComplexFormModal", vm);
=======
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
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
        return View("Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ComplexFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
<<<<<<< HEAD
            if (IsAjaxRequest()) return JsonValidationErrors();
=======
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
            await PopulateFormOptions(model);
            return View("Form", model);
        }

<<<<<<< HEAD
        try
        {
            await ApplyImageUploadAsync(model);
            await courtService.UpdateComplexAsync(id, MapToDto(model));
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Cập nhật tổ hợp sân thành công!" });
            TempData["Success"] = "Cập nhật tổ hợp sân thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
            {
                var status = ex.Message.Contains("401", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("xác thực", StringComparison.OrdinalIgnoreCase)
                    ? 401 : 400;
                return StatusCode(status, new { success = false, message = ex.Message });
            }
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateFormOptions(model);
            return View("Form", model);
        }
=======
        await courtService.UpdateComplexAsync(id, MapToDto(model));
        TempData["Success"] = "Cập nhật tổ hợp sân thành công!";
        return RedirectToAction(nameof(Index));
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
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
<<<<<<< HEAD
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Xóa tổ hợp sân thành công!" });
=======
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
            TempData["Success"] = "Xóa tổ hợp sân thành công!";
        }
        catch (InvalidOperationException ex)
        {
<<<<<<< HEAD
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });
=======
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

<<<<<<< HEAD
    private bool IsAjaxRequest() =>
        Request.Headers.XRequestedWith == "XMLHttpRequest";

    private JsonResult JsonValidationErrors()
    {
        var errors = ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                k => k.Key,
                v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var firstMessage = errors.Values.SelectMany(v => v).FirstOrDefault()
            ?? "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";

        return Json(new { success = false, message = firstMessage, errors });
    }

    private async Task<ComplexFormViewModel?> BuildFormViewModelForEdit(int id)
    {
        var complex = await courtService.GetComplexByIdAsync(id);
        if (complex == null) return null;

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
        return vm;
    }

=======
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
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

<<<<<<< HEAD
    private async Task ApplyImageUploadAsync(ComplexFormViewModel model)
    {
        if (model.ImageFile is { Length: > 0 })
        {
            if (model.ImageFile.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");

            model.ImageUrl = await courtService.UploadComplexImageAsync(model.ImageFile);
        }
    }

=======
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e
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
