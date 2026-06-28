using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(IUserService userService) : Controller
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
            TotalPages = result.TotalPages
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

        return View(new UserEditRolesViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(int id, UserEditRolesViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        await userService.UpdateUserRoleAsync(id, model.Role);
        await userService.ToggleUserStatusAsync(id, model.IsActive);
        TempData["Success"] = "Cập nhật vai trò người dùng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, bool isActive)
    {
        await userService.ToggleUserStatusAsync(id, isActive);
        TempData["Success"] = isActive ? "Đã kích hoạt tài khoản." : "Đã vô hiệu hóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }
}
