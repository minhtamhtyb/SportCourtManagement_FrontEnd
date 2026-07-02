using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.ViewModels.Admin;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class RolesController(IRoleService roleService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new RoleListViewModel
        {
            Roles = await roleService.GetRolesAsync(),
            PermissionMatrix = await roleService.GetPermissionMatrixAsync()
        };
        return View(vm);
    }

    public async Task<IActionResult> PermissionMatrix()
    {
        var vm = new RoleListViewModel
        {
            PermissionMatrix = await roleService.GetPermissionMatrixAsync()
        };
        return View(vm);
    }
}
