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
        var result = await userService.GetUsersAsync(search, role, page, 10);
        var vm = new UserListViewModel
        {
            Users = result.Items,
            Search = search,
            Role = role,
            Page = page,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            RoleOptions = (await roleService.GetRolesAsync()).Select(r => r.RoleName).ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    public async Task<IActionResult> EditRoles(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        var vm = new UserEditRolesViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            RoleOptions = await GetRoleOptionsAsync(),
            IsSelf = GetCurrentUserId() == user.UserId
        };

        if (IsAjaxRequest()) return PartialView("_UserEditRolesModal", vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(int id, UserEditRolesViewModel model)
    {
        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest()) return JsonValidationErrors();
            model.RoleOptions = await GetRoleOptionsAsync();
            model.IsSelf = GetCurrentUserId() == id;
            return View(model);
        }

        try
        {
            if (GetCurrentUserId() == id && model.Role != "Admin")
                throw new InvalidOperationException("Bạn không thể tự hạ quyền Admin của chính mình.");
            if (GetCurrentUserId() == id && !model.IsActive)
                throw new InvalidOperationException("Bạn không thể tự vô hiệu hóa tài khoản của chính mình.");

            await userService.UpdateUserAccessAsync(id, model.Role, model.IsActive);

            if (IsAjaxRequest())
                return Json(new { success = true, message = "Cập nhật vai trò người dùng thành công!" });
            TempData["Success"] = "Cập nhật vai trò người dùng thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (IsAjaxRequest())
                return StatusCode(400, new { success = false, message = ex.Message });
            ModelState.AddModelError(string.Empty, ex.Message);
            model.RoleOptions = await GetRoleOptionsAsync();
            model.IsSelf = GetCurrentUserId() == id;
            return View(model);
        }
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
