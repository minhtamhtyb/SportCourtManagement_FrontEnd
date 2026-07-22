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
        string? complexName = null;
        if (complexId > 0)
        {
            var complex = await courtService.GetComplexByIdAsync(complexId);
            complexName = complex?.ComplexName;
        }

        var vm = await BuildFormViewModel(complexId, complexName);
        if (IsAjaxRequest()) return PartialView("_CourtFormModal", vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int complexId, CourtFormViewModel model)
    {
        try
        {
            await ApplyImageUploadAsync(model);
            await ValidateCourtCode(model);

            // Remove ImageUrl from ModelState validation since it is populated via file upload
            ModelState.Remove(nameof(model.ImageUrl));

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest()) return JsonValidationErrors();
                await PopulateFormOptions(model, complexId);
                return View("Create", model);
            }

            if (!string.IsNullOrEmpty(model.CloseTime) && !string.IsNullOrEmpty(model.OpenTime) && model.CloseTime.CompareTo(model.OpenTime) <= 0)
            {
                if (IsAjaxRequest()) return BadRequest(new { success = false, message = "Giờ đóng cửa phải sau giờ mở cửa." });
                ModelState.AddModelError(nameof(model.CloseTime), "Giờ đóng cửa phải sau giờ mở cửa.");
                await PopulateFormOptions(model, complexId);
                return View("Create", model);
            }

            var targetComplexId = complexId > 0 ? complexId : model.ComplexId;
            await courtService.CreateCourtAsync(MapToDto(model, targetComplexId));
            if (IsAjaxRequest())
                return Json(new { success = true, message = "Thêm sân mới thành công!" });
            TempData["Success"] = "Thêm sân mới thành công!";
            return RedirectToAction("Details", "Complexes", new { id = targetComplexId });
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
        try
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
            vm.ImageUrls = court.ImageUrls ?? (string.IsNullOrEmpty(court.ImageUrl) ? [] : [court.ImageUrl]);
            vm.Status = court.Status;
            vm.OpenTime = !string.IsNullOrEmpty(court.OpenTime) && court.OpenTime.Length >= 5 ? court.OpenTime[..5] : "06:00";
            vm.CloseTime = !string.IsNullOrEmpty(court.CloseTime) && court.CloseTime.Length >= 5 ? court.CloseTime[..5] : "22:00";
            vm.PricePerHour = court.PricePerHour;
            vm.CourtSize = court.CourtSize;
            vm.Pricings = BuildDefaultPricings(court.PricePerHour, court.Pricings);

            if (IsAjaxRequest()) return PartialView("_CourtFormModal", vm);
            return View("Create", vm);
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest()) return StatusCode(500, new { success = false, message = "Lỗi khi tải thông tin sân: " + ex.Message });
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourtFormViewModel model)
    {
        try
        {
            var court = await courtService.GetCourtByIdAsync(id);
            if (court == null) return NotFound();

            await ApplyImageUploadAsync(model);
            await ValidateCourtCode(model);

            // Remove ImageUrl from ModelState validation since it is populated via file upload
            ModelState.Remove(nameof(model.ImageUrl));

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest()) return JsonValidationErrors();
                await PopulateFormOptions(model, model.ComplexId);
                return View("Create", model);
            }

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
        vm.Pricings = BuildDefaultPricings(vm.PricePerHour);
        return vm;
    }

    private async Task PopulateFormOptions(CourtFormViewModel vm, int complexId)
    {
        vm.ComplexId = complexId;
        vm.CourtTypes = await courtService.GetCourtTypesAsync();
        if (vm.Pricings == null || !vm.Pricings.Any())
        {
            vm.Pricings = BuildDefaultPricings(vm.PricePerHour);
        }
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
        ImageUrls = model.ImageUrls,
        Status = model.Status,
        OpenTime = model.OpenTime,
        CloseTime = model.CloseTime,
        PricePerHour = model.PricePerHour,
        CourtSize = model.CourtSize,
        Pricings = model.Pricings
    };

    private static List<CourtPricingInputDto> BuildDefaultPricings(decimal pricePerHour, List<CourtPricingInputDto>? existingPricings = null)
    {
        var standardSlots = new[]
        {
            new { SlotId = 1, SlotName = "Slot 1 (06:00 - 07:30)", StartTime = "06:00", EndTime = "07:30", Duration = 1.5m },
            new { SlotId = 2, SlotName = "Slot 2 (07:30 - 09:00)", StartTime = "07:30", EndTime = "09:00", Duration = 1.5m },
            new { SlotId = 3, SlotName = "Slot 3 (09:00 - 10:30)", StartTime = "09:00", EndTime = "10:30", Duration = 1.5m },
            new { SlotId = 4, SlotName = "Slot 4 (15:00 - 16:30)", StartTime = "15:00", EndTime = "16:30", Duration = 1.5m },
            new { SlotId = 5, SlotName = "Slot 5 (16:30 - 18:00)", StartTime = "16:30", EndTime = "18:00", Duration = 1.5m },
            new { SlotId = 6, SlotName = "Slot 6 (18:00 - 19:30)", StartTime = "18:00", EndTime = "19:30", Duration = 1.5m },
            new { SlotId = 7, SlotName = "Slot 7 (19:30 - 21:00)", StartTime = "19:30", EndTime = "21:00", Duration = 1.5m },
            new { SlotId = 8, SlotName = "Slot 8 (21:00 - 22:30)", StartTime = "21:00", EndTime = "22:30", Duration = 1.5m },
        };

        var list = new List<CourtPricingInputDto>();
        foreach (var slot in standardSlots)
        {
            var match = existingPricings?.FirstOrDefault(p => p.SlotId == slot.SlotId);
            decimal price = match != null && match.Price > 0 
                ? match.Price 
                : (pricePerHour > 0 ? pricePerHour * slot.Duration : 150000);

            list.Add(new CourtPricingInputDto
            {
                SlotId = slot.SlotId,
                SlotName = slot.SlotName,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                Price = price
            });
        }
        return list;
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

    private async Task ApplyImageUploadAsync(CourtFormViewModel model)
    {
        model.ImageUrls ??= new List<string>();

        // Handle single file if provided
        if (model.ImageFile is { Length: > 0 })
        {
            if (model.ImageFile.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");
            var uploadedUrl = await courtService.UploadComplexImageAsync(model.ImageFile);
            model.ImageUrl = uploadedUrl;
            if (!model.ImageUrls.Contains(uploadedUrl))
            {
                model.ImageUrls.Add(uploadedUrl);
            }
        }

        // Handle multiple files if provided
        if (model.ImageFiles != null && model.ImageFiles.Any())
        {
            foreach (var file in model.ImageFiles.Where(f => f.Length > 0))
            {
                if (file.Length > 5 * 1024 * 1024)
                    throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");
                var uploadedUrl = await courtService.UploadComplexImageAsync(file);
                if (!model.ImageUrls.Contains(uploadedUrl))
                {
                    model.ImageUrls.Add(uploadedUrl);
                }
            }
            if (string.IsNullOrEmpty(model.ImageUrl) && model.ImageUrls.Any())
            {
                model.ImageUrl = model.ImageUrls.First();
            }
        }

        // Limit to max 3 images
        if (model.ImageUrls.Count > 3)
        {
            model.ImageUrls = model.ImageUrls.Take(3).ToList();
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
