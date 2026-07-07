using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SportCourtManagement_FrontEnd.Models.Auth;
using SportCourtManagement_FrontEnd.Models.ViewModels.Auth;
using SportCourtManagement_FrontEnd.Services;

namespace SportCourtManagement_FrontEnd.Controllers;

public class AccountController : Controller
{
    private readonly ICourtApiService _apiService;

    public AccountController(ICourtApiService apiService)
    {
        _apiService = apiService;
    }

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

        var result = await _apiService.LoginAsync(new LoginRequest
        {
            Email = model.Email,
            Password = model.Password
        });

        if (!result.Succeeded)
        {
            if (result.RequiresEmailVerification)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Tài khoản chưa xác thực email.");
                TempData["VerifyEmail"] = model.Email;
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Email hoặc mật khẩu không đúng.");
            }
            return View(model);
        }

        await SignInUserAsync(result.Response!.User, result.Response.AccessToken);
        TempData["SuccessMessage"] = $"Chào mừng {result.Response.User.FullName} quay trở lại!";

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

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

        var (success, error) = await _apiService.RegisterAsync(new RegisterRequest
        {
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword
        });

        if (success)
        {
            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email hoặc console Backend để lấy mã OTP.";
            return RedirectToAction(nameof(VerifyEmail), new { email = model.Email });
        }

        ModelState.AddModelError(string.Empty, error ?? "Đăng ký thất bại.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult VerifyEmail(string? email)
    {
        return View(new VerifyEmailViewModel { Email = email ?? TempData["VerifyEmail"]?.ToString() ?? "" });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error) = await _apiService.VerifyEmailAsync(new VerifyEmailRequest
        {
            Email = model.Email,
            Otp = model.Otp
        });

        if (success)
        {
            TempData["SuccessMessage"] = "Xác thực email thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError(string.Empty, error ?? "Mã OTP không đúng hoặc đã hết hạn.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("jwt");
        Response.Cookies.Delete("AccessToken");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["SuccessMessage"] = "Đăng xuất thành công!";
        return RedirectToAction("Index", "Courts");
    }

    private async Task SignInUserAsync(UserDto user, string accessToken)
    {
        // Ghi cookie jwt cho các chức năng hiện tại trong hệ thống
        Response.Cookies.Append("jwt", accessToken, new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTimeOffset.UtcNow.AddHours(12),
            Path = "/"
        });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("jwt_access_token", accessToken)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });
    }

    private IActionResult RedirectToRoleHome(string? role = null)
    {
        role ??= User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin" || role == "Staff")
        {
            return RedirectToAction("AdminIndex", "Bookings");
        }
        return RedirectToAction("Index", "Courts");
    }
}
