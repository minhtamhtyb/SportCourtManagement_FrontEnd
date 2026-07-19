using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;
using System.Security.Claims;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(IUserService userService, IRoleService roleService) : Controller
{
    public async Task<IActionResult> Index(string? search, string? role, int page = 1)
    {
        try
        {
            var result = await userService.GetUsersAsync(search, role, page, 10);
            var roles = (await roleService.GetRolesAsync()).Select(r => r.RoleName).ToList();

            var vm = new UserListViewModel
            {
                Users = result.Items,
                Search = search,
                Role = role,
                Page = page,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,
                RoleOptions = roles
            };
            return View(vm);
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(new UserListViewModel
            {
                Users = [],
                Search = search,
                Role = role,
                Page = page,
                TotalCount = 0,
                TotalPages = 0,
                RoleOptions = []
            });
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new UserFormViewModel();
        if (IsAjaxRequest()) return PartialView("_UserFormModal", vm);
        return View("Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Mật khẩu là bắt buộc khi tạo mới người dùng.");
        }

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            return View("Form", model);
        }

        try
        {
            await userService.CreateUserAsync(new UserDto
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Role = model.Role,
                Gender = model.Gender,
                SkillLevel = model.SkillLevel,
                IsActive = model.IsActive
            }, model.Password!);

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Thêm người dùng mới thành công!" });
            TempData["Success"] = "Thêm người dùng mới thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return StatusCode(400, new { success = false, message = ex.Message });
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        var vm = new UserFormViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone ?? "",
            Role = user.Role,
            Gender = user.Gender ?? "Other",
            SkillLevel = user.SkillLevel ?? "Beginner",
            IsActive = user.IsActive,
            IsSelf = GetCurrentUserId() == user.UserId
        };

        if (IsAjaxRequest()) return PartialView("_UserFormModal", vm);
        return View("Form", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            return View("Form", model);
        }

        try
        {
            if (GetCurrentUserId() == id && model.Role != "Admin")
                throw new InvalidOperationException("Bạn không thể tự hạ quyền Admin của chính mình.");
            if (GetCurrentUserId() == id && !model.IsActive)
                throw new InvalidOperationException("Bạn không thể tự vô hiệu hóa tài khoản của chính mình.");

            await userService.UpdateUserByAdminAsync(id, new UserDto
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Role = model.Role,
                Gender = model.Gender,
                SkillLevel = model.SkillLevel,
                IsActive = model.IsActive
            });

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Cập nhật thông tin người dùng thành công!" });
            TempData["Success"] = "Cập nhật thông tin người dùng thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return StatusCode(400, new { success = false, message = ex.Message });
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (GetCurrentUserId() == id)
                throw new InvalidOperationException("Bạn không thể tự xóa tài khoản của chính mình.");

            await userService.DeleteUserAsync(id);

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Xóa người dùng thành công!" });
            TempData["Success"] = "Xóa người dùng thành công!";
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, bool isActive)
    {
        try
        {
            if (GetCurrentUserId() == id && !isActive)
                throw new InvalidOperationException("Bạn không thể tự vô hiệu hóa tài khoản của chính mình.");

            await userService.ToggleUserStatusAsync(id, isActive);
            var message = isActive ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản.";

            if (IsAjaxRequest())
                return Json(new { success = true, message });
            TempData["Success"] = message;
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return Json(new { success = false, message = ex.Message });
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<string>> GetRoleOptionsAsync() =>
        (await roleService.GetRolesAsync()).Select(r => r.RoleName).ToList();

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
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
}
