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
        if (IsAjaxRequest()) return PartialView("_CourtFormModal", vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int complexId, CourtFormViewModel model)
    {
        await ValidateCourtCode(model);

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            await PopulateFormOptions(model, complexId);
            return View("Create", model);
        }

        if (model.CloseTime.CompareTo(model.OpenTime) <= 0)
        {
            if (IsAjaxRequest()) return BadRequest(new { success = false, message = "Giờ đóng cửa phải sau giờ mở cửa." });
            ModelState.AddModelError(nameof(model.CloseTime), "Giờ đóng cửa phải sau giờ mở cửa.");
            await PopulateFormOptions(model, complexId);
            return View("Create", model);
        }

        try
        {
            await ApplyImageUploadAsync(model);
            await courtService.CreateCourtAsync(MapToDto(model, complexId));
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Thêm sân mới thành công!" });
            TempData["Success"] = "Thêm sân mới thành công!";
            return RedirectToAction("Details", "Complexes", new { id = complexId });
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest()) return StatusCode(400, new { success = false, message = ex.Message });
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateFormOptions(model, complexId);
            return View("Create", model);
        }
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
        if (IsAjaxRequest()) return PartialView("_CourtFormModal", vm);
        return View("Create", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourtFormViewModel model)
    {
        var court = await courtService.GetCourtByIdAsync(id);
        if (court == null) return NotFound();

        await ValidateCourtCode(model);

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            await PopulateFormOptions(model, model.ComplexId);
            return View("Create", model);
        }

        try
        {
            await ApplyImageUploadAsync(model);
            await courtService.UpdateCourtAsync(id, MapToDto(model, model.ComplexId));
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Cập nhật sân thành công!" });
            TempData["Success"] = "Cập nhật sân thành công!";
            return RedirectToAction("Details", "Complexes", new { id = model.ComplexId });
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest()) return StatusCode(400, new { success = false, message = ex.Message });
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateFormOptions(model, model.ComplexId);
            return View("Create", model);
        }
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

    private async Task ApplyImageUploadAsync(CourtFormViewModel model)
    {
        if (model.ImageFile is { Length: > 0 })
        {
            if (model.ImageFile.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");
            model.ImageUrl = await courtService.UploadComplexImageAsync(model.ImageFile);
        }
    }

    private async Task ValidateCourtCode(CourtFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.CourtCode)) return;

        var types = await courtService.GetCourtTypesAsync();
        var courtType = types.FirstOrDefault(t => t.CourtTypeId == model.CourtTypeId);
        if (courtType != null)
        {
            string typeName = courtType.TypeName.ToLowerInvariant().Trim();
            string code = model.CourtCode.Trim().ToUpperInvariant();
            
            bool isValid = false;
            string requiredPrefix = "";
            string suggestion = "";
            
            if (typeName.Contains("cầu lông"))
            {
                requiredPrefix = "CL";
                isValid = code.StartsWith("CL");
                suggestion = "CL-01 hoặc CL-A1";
            }
            else if (typeName.Contains("bóng đá"))
            {
                requiredPrefix = "BD";
                isValid = code.StartsWith("BD");
                suggestion = "BD-01 hoặc BD-A1";
            }
            else if (typeName.Contains("pickleball"))
            {
                requiredPrefix = "PB, PK hoặc PCK";
                isValid = code.StartsWith("PB") || code.StartsWith("PK") || code.StartsWith("PCK");
                suggestion = "PB-01, PK-A1 hoặc PCK-A1";
            }
            else if (typeName.Contains("tennis"))
            {
                requiredPrefix = "TN hoặc TEN";
                isValid = code.StartsWith("TN") || code.StartsWith("TEN");
                suggestion = "TN-01 hoặc TEN-A1";
            }
            else if (typeName.Contains("bóng rổ"))
            {
                requiredPrefix = "BR";
                isValid = code.StartsWith("BR");
                suggestion = "BR-01 hoặc BR-A1";
            }
            else
            {
                isValid = code.Length >= 2 && char.IsLetter(code[0]) && char.IsLetter(code[1]);
                requiredPrefix = "ít nhất 2 chữ cái";
                suggestion = "VD: SAN-01";
            }

            if (!isValid)
            {
                ModelState.AddModelError(nameof(model.CourtCode), $"Mã sân của loại '{courtType.TypeName}' phải bắt đầu bằng '{requiredPrefix}' (Ví dụ: {suggestion}).");
            }
        }
    }
}
