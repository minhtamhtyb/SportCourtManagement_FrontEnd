using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportCourtManagement_FrontEnd.Models.Configuration;

using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Auth;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Controllers;

public class AccountController(IAuthService authService, IOptions<ApiSettings> apiSettings) : Controller
{
    private readonly bool _useMockData = apiSettings.Value.UseMockData;

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

        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (!result.Succeeded)
        {
            if (result.RequiresEmailVerification)
            {
                TempData["Error"] = result.ErrorMessage ?? "Tài khoản chưa xác thực email.";
                TempData["VerifyEmail"] = model.Email;
            }
            else
            {
                TempData["Error"] = result.ErrorMessage ?? "Email hoặc mật khẩu không chính xác.";
            }
            return View(model);
        }

        await SignInUserAsync(result.Response!.User, result.Response.AccessToken, result.Response.RefreshToken);
        TempData["Success"] = $"Chào mừng {result.Response.User.FullName} quay trở lại!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToRoleHome(result.Response.User.Role);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GoogleLogin(string idToken, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            TempData["Error"] = "Token xác thực Google không hợp lệ.";
            return RedirectToAction(nameof(Login));
        }

        var result = await authService.GoogleLoginAsync(idToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage ?? "Đăng nhập bằng Google thất bại.";
            return RedirectToAction(nameof(Login));
        }

        await SignInUserAsync(result.Response!.User, result.Response.AccessToken, result.Response.RefreshToken);
        TempData["Success"] = $"Chào mừng {result.Response.User.FullName} quay trở lại!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToRoleHome(result.Response.User.Role);
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

            if (_useMockData)
            {
                TempData["Success"] = "Đăng ký thành công! Demo OTP: 123456";
                return RedirectToAction(nameof(VerifyEmail), new { email = model.Email });
            }
            else
            {
                TempData["Success"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return View(model);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Không kết nối được API Backend. Hãy chạy Backend trước (port 5000).";
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyEmail(string? email)
    {
        ViewBag.UseMockData = _useMockData;
        return View(new VerifyEmailViewModel { Email = email ?? TempData["VerifyEmail"]?.ToString() ?? "" });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
    {
        ViewBag.UseMockData = _useMockData;

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
            TempData["Error"] = ex.Message;
            return View(model);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Không kết nối được API Backend. Hãy chạy Backend trước (port 5000).";
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync();
        ClearAuthSession();
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
        if (_useMockData)
        {
            var user = new UserDto
            {
                UserId = 1,
                FullName = "Super Admin",
                Email = "admin@sportscourtms.vn",
                Role = "Admin",
                MembershipTier = "Platinum"
            };
            await SignInUserAsync(user, "dev-token", "dev-refresh");
            TempData["Success"] = "Dev mode — Đăng nhập Admin thành công!";
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = "admin@sportscourtms.vn",
            Password = "Admin@123"
        });

        if (!result.Succeeded)
        {
            TempData["Error"] = result.ErrorMessage ?? "Không thể đăng nhập Admin. Kiểm tra Backend và DB seed.";
            return RedirectToAction(nameof(Login));
        }

        await SignInUserAsync(result.Response!.User, result.Response.AccessToken, result.Response.RefreshToken);
        TempData["Success"] = "Đăng nhập Admin thành công!";
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    private async Task SignInUserAsync(UserDto user, string accessToken, string? refreshToken = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(Services.Api.JwtForwardingHandler.AccessTokenClaimType, accessToken),
        };

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            AllowRefresh = true
        };

        var tokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token", Value = accessToken }
        };
        if (!string.IsNullOrWhiteSpace(refreshToken))
            tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });

        authProperties.StoreTokens(tokens);

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authProperties);

        await HttpContext.Session.LoadAsync();
        HttpContext.Session.SetString(Services.Api.JwtForwardingHandler.SessionTokenKey, accessToken);
        if (!string.IsNullOrWhiteSpace(refreshToken))
            HttpContext.Session.SetString("refresh_token", refreshToken);
        await HttpContext.Session.CommitAsync();
    }

    private void ClearAuthSession()
    {
        HttpContext.Session.Remove(Services.Api.JwtForwardingHandler.SessionTokenKey);
        HttpContext.Session.Remove("refresh_token");
    }

    private IActionResult RedirectToRoleHome(string? role = null)
    {
        role ??= User.FindFirst(ClaimTypes.Role)?.Value;
        return role switch
        {
            "Admin" or "Staff" or "Coach" => RedirectToAction("Index", "Dashboard", new { area = "Admin" }),
            "Manager" => RedirectToAction("Shifts", "Manager"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
