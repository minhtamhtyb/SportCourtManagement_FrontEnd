using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOrStaff")]
public class ServicesController(IServiceCatalogService serviceCatalog) : Controller
{
    private const int PageSize = 8;

    public async Task<IActionResult> Index(string? category, string? search, int page = 1)
    {
        if (page < 1) page = 1;

        var allServices = await serviceCatalog.GetServicesAsync(category, search);

        int totalItems = allServices.Count;
        int totalPages = (int)Math.Ceiling((double)totalItems / PageSize);
        if (totalPages < 1) totalPages = 1;
        if (page > totalPages) page = totalPages;

        var paginatedServices = allServices
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var vm = new ServiceListViewModel
        {
            Services = paginatedServices,
            Category = category,
            Search = search,
            PageNumber = page,
            PageSize = PageSize,
            TotalPages = totalPages,
            TotalItems = totalItems
        };
        return View(vm);
    }

    // GET api for modal - get service by id as JSON
    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var svc = await serviceCatalog.GetServiceByIdAsync(id);
        if (svc == null) return NotFound(new { success = false, message = "Không tìm thấy dịch vụ." });
        return Json(new { success = true, data = svc });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] ServiceFormDto model)
    {
        if (!TryValidateModel(model))
        {
            var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                .Select(x => x.Value!.Errors.First().ErrorMessage).ToList();
            return BadRequest(new { success = false, message = errors.FirstOrDefault() ?? "Dữ liệu không hợp lệ." });
        }

        try
        {
            await serviceCatalog.CreateServiceAsync(new ServiceDto
            {
                ServiceName = model.ServiceName,
                Category = model.Category,
                Price = model.Price,
                Unit = model.Unit,
                Description = model.Description,
                StockQty = model.StockQty,
                IsActive = model.IsActive
            });
            return Json(new { success = true, message = "Thêm dịch vụ thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [FromBody] ServiceFormDto model)
    {
        if (!TryValidateModel(model))
        {
            var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                .Select(x => x.Value!.Errors.First().ErrorMessage).ToList();
            return BadRequest(new { success = false, message = errors.FirstOrDefault() ?? "Dữ liệu không hợp lệ." });
        }

        try
        {
            await serviceCatalog.UpdateServiceAsync(id, new ServiceDto
            {
                ServiceName = model.ServiceName,
                Category = model.Category,
                Price = model.Price,
                Unit = model.Unit,
                Description = model.Description,
                StockQty = model.StockQty,
                IsActive = model.IsActive
            });
            return Json(new { success = true, message = "Cập nhật dịch vụ thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await serviceCatalog.DeleteServiceAsync(id);
            return Json(new { success = true, message = "Vô hiệu hóa dịch vụ thành công!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

// DTO for JSON body binding from modal forms
public class ServiceFormDto
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Tên dịch vụ không được để trống")]
    [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2, ErrorMessage = "Tên dịch vụ phải từ 2 đến 100 ký tự")]
    public string ServiceName { get; set; } = "";

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn loại dịch vụ")]
    public string Category { get; set; } = "Equipment";

    [System.ComponentModel.DataAnnotations.Range(0, 100_000_000, ErrorMessage = "Giá phải từ 0 đến 100,000,000")]
    public decimal Price { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Đơn vị không được để trống")]
    public string Unit { get; set; } = "";

    [System.ComponentModel.DataAnnotations.StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 100_000, ErrorMessage = "Tồn kho phải từ 0 đến 100,000")]
    public int StockQty { get; set; }

    public bool IsActive { get; set; } = true;
}
