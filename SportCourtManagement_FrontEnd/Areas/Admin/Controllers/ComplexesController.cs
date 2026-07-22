using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class ComplexesController(
    ICourtService courtService,
    IServiceCatalogService serviceCatalog,
    IComplexServiceOfferingService offeringService) : Controller
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
        var vm = await BuildFormViewModel();
        if (IsAjaxRequest()) return PartialView("_ComplexFormModal", vm);
        return View("Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplexFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            await PopulateFormOptions(model);
            return View("Form", model);
        }

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
                    ? 401 : 400;
                return StatusCode(status, new { success = false, message = ex.Message });
            }
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateFormOptions(model);
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var vm = await BuildFormViewModelForEdit(id);
        if (vm == null) return NotFound();
        if (IsAjaxRequest()) return PartialView("_ComplexFormModal", vm);
        return View("Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ComplexFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            await PopulateFormOptions(model);
            return View("Form", model);
        }

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
                var status = ex.Message.Contains("401", StringComparison.OrdinalIgnoreCase) ? 401 : 400;
                return StatusCode(status, new { success = false, message = ex.Message });
            }
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateFormOptions(model);
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Details(int id, string? search, string? status)
    {
        var complex = await courtService.GetComplexByIdAsync(id);
        if (complex == null) return NotFound();

        var allCourtTypes = await courtService.GetCourtTypesAsync();
        var complexCourtTypes = allCourtTypes
            .Where(t => complex.CourtTypeIds.Contains(t.CourtTypeId))
            .ToList();

        var vm = new ComplexDetailsViewModel
        {
            Complex = complex,
            Manager = complex.ManagerId.HasValue
                ? await courtService.GetManagerByIdAsync(complex.ManagerId.Value)
                : null,
            Courts = await courtService.GetCourtsByComplexAsync(id, search, status),
            CourtTypes = allCourtTypes,
            ComplexCourtTypes = complexCourtTypes,
            Search = search,
            StatusFilter = status
        };
        return View(vm);
    }

    public IActionResult Services(int id, int? courtTypeId)
    {
        return RedirectToAction("Index", "Services", new { area = "Admin" });
    }

    public async Task<IActionResult> AddService(int complexId, int? courtTypeId)
    {
        var complex = await courtService.GetComplexByIdAsync(complexId);
        if (complex == null) return NotFound();

        var allCourtTypes = await courtService.GetCourtTypesAsync();
        var complexCourtTypes = allCourtTypes.Where(t => complex.CourtTypeIds.Contains(t.CourtTypeId)).ToList();
        if (complexCourtTypes.Count == 0)
        {
            if (IsAjaxRequest())
                return BadRequest(new { success = false, message = "Tổ hợp chưa có sân — hãy thêm sân trước khi cấu hình dịch vụ." });
            TempData["Error"] = "Tổ hợp chưa có sân — hãy thêm sân trước khi cấu hình dịch vụ.";
            return RedirectToAction(nameof(Services), new { id = complexId });
        }

        var vm = new ServiceOfferingFormViewModel
        {
            ComplexId = complexId,
            CourtTypeId = courtTypeId ?? complexCourtTypes[0].CourtTypeId,
            CourtTypeOptions = complexCourtTypes,
            CatalogServices = await serviceCatalog.GetServicesAsync(null, null)
        };

        if (IsAjaxRequest()) return PartialView("_ServiceFormModal", vm);
        return View("ServiceForm", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService(ServiceOfferingFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            await PopulateServiceFormOptions(model);
            return View("ServiceForm", model);
        }

        try
        {
            var catalog = (await serviceCatalog.GetServicesAsync(null, null))
                .FirstOrDefault(s => s.ServiceId == model.ServiceId);
            var price = model.ServiceMode == "Included" ? 0 : (model.Price > 0 ? model.Price : catalog?.Price ?? 0);

            await offeringService.CreateAsync(model.ComplexId, model.CourtTypeId, new ComplexCourtTypeServiceDto
            {
                ServiceId = model.ServiceId,
                Price = price,
                StockQty = model.StockQty,
                ServiceMode = model.ServiceMode,
                IsActive = model.IsActive
            });

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Thêm dịch vụ cho loại sân thành công!" });
            TempData["Success"] = "Thêm dịch vụ cho loại sân thành công!";
            return RedirectToAction(nameof(Services), new { id = model.ComplexId, courtTypeId = model.CourtTypeId });
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return StatusCode(400, new { success = false, message = ex.Message });
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateServiceFormOptions(model);
            return View("ServiceForm", model);
        }
    }

    public async Task<IActionResult> EditService(int offeringId, int complexId)
    {
        var offerings = await offeringService.GetByComplexAsync(complexId);
        var offering = offerings.FirstOrDefault(o => o.OfferingId == offeringId);
        if (offering == null) return NotFound();

        var complex = await courtService.GetComplexByIdAsync(complexId);
        if (complex == null) return NotFound();

        var vm = new ServiceOfferingFormViewModel
        {
            ComplexId = complexId,
            OfferingId = offeringId,
            CourtTypeId = offering.CourtTypeId,
            ServiceId = offering.ServiceId,
            Price = offering.Price,
            StockQty = offering.StockQty,
            ServiceMode = offering.ServiceMode,
            IsActive = offering.IsActive,
            CourtTypeOptions = (await courtService.GetCourtTypesAsync())
                .Where(t => complex.CourtTypeIds.Contains(t.CourtTypeId)).ToList(),
            CatalogServices = await serviceCatalog.GetServicesAsync(null, null)
        };

        if (IsAjaxRequest()) return PartialView("_ServiceFormModal", vm);
        return View("ServiceForm", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditService(int offeringId, ServiceOfferingFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            await PopulateServiceFormOptions(model);
            return View("ServiceForm", model);
        }

        try
        {
            var price = model.ServiceMode == "Included" ? 0 : model.Price;
            await offeringService.UpdateAsync(offeringId, new ComplexCourtTypeServiceDto
            {
                Price = price,
                StockQty = model.StockQty,
                ServiceMode = model.ServiceMode,
                IsActive = model.IsActive
            });

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Cập nhật dịch vụ thành công!" });
            TempData["Success"] = "Cập nhật dịch vụ thành công!";
            return RedirectToAction(nameof(Services), new { id = model.ComplexId, courtTypeId = model.CourtTypeId });
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return StatusCode(400, new { success = false, message = ex.Message });
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateServiceFormOptions(model);
            return View("ServiceForm", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteService(int offeringId, int complexId, int? courtTypeId)
    {
        try
        {
            await offeringService.DeleteAsync(offeringId);
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Đã xóa dịch vụ khỏi loại sân." });
            TempData["Success"] = "Đã xóa dịch vụ khỏi loại sân.";
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Services), new { id = complexId, courtTypeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await courtService.DeleteComplexAsync(id);
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Xóa tổ hợp sân thành công!" });
            TempData["Success"] = "Xóa tổ hợp sân thành công!";
        }
        catch (InvalidOperationException ex)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

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

    private async Task PopulateServiceFormOptions(ServiceOfferingFormViewModel model)
    {
        var complex = await courtService.GetComplexByIdAsync(model.ComplexId);
        model.CourtTypeOptions = (await courtService.GetCourtTypesAsync())
            .Where(t => complex?.CourtTypeIds.Contains(t.CourtTypeId) == true).ToList();
        model.CatalogServices = await serviceCatalog.GetServicesAsync(null, null);
    }

    private async Task ApplyImageUploadAsync(ComplexFormViewModel model)
    {
        if (model.ImageFile is { Length: > 0 })
        {
            if (model.ImageFile.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");
            model.ImageUrl = await courtService.UploadComplexImageAsync(model.ImageFile);
        }
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
