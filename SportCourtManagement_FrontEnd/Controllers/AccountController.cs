using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Auth;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Controllers;

public class AccountController(IAuthService authService) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleHome();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await authService.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (response == null)
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        await SignInUserAsync(response.User, response.AccessToken);
        TempData["Success"] = $"Chào mừng {response.User.FullName} quay trở lại!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToRoleHome(response.User.Role);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await authService.RegisterAsync(new RegisterRequest
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword
            });
            TempData["Success"] = "Đăng ký thành công! Mã OTP demo: 123456";
            return RedirectToAction(nameof(VerifyEmail), new { email = model.Email });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyEmail(string? email) =>
        View(new VerifyEmailViewModel { Email = email ?? "" });

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await authService.VerifyEmailAsync(new VerifyEmailRequest
            {
                Email = model.Email,
                Otp = model.Otp
            });
            TempData["Success"] = "Xác thực email thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "Đăng xuất thành công!";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevLoginAdmin()
    {
        var user = new UserDto
        {
            UserId = 1,
            FullName = "Super Admin",
            Email = "admin@sportscourtms.vn",
            Role = "Admin",
            MembershipTier = "Platinum"
        };
        await SignInUserAsync(user, "dev-token");
        TempData["Success"] = "Dev mode — Đăng nhập Admin thành công!";
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    private async Task SignInUserAsync(UserDto user, string token)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("access_token", token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    private IActionResult RedirectToRoleHome(string? role = null)
    {
        role ??= User.FindFirst(ClaimTypes.Role)?.Value;
        return role is "Admin" or "Staff" or "Coach"
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index", "Home");
    }
}
