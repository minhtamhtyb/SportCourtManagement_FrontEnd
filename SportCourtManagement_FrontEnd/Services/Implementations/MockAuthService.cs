using Microsoft.AspNetCore.Http;
using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Models.ViewModels.Auth;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class MockAuthService(MockDataStore store, IHttpContextAccessor httpContextAccessor)
    : IAuthService
{
    public Task<AuthLoginResult> LoginAsync(LoginRequest request)
    {
        var user = store.Users.FirstOrDefault(u =>
            u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)
        );

        if (
            user == null
            || !store.Passwords.TryGetValue(request.Email, out var pwd)
            || pwd != request.Password
        )
            return Task.FromResult(AuthLoginResult.Fail("Email hoặc mật khẩu không đúng."));

        if (!user.IsActive)
            return Task.FromResult(AuthLoginResult.Fail("Tài khoản đã bị khóa."));

        return Task.FromResult(
            AuthLoginResult.Ok(
                new AuthResponse
                {
                    AccessToken = $"mock-token-{user.UserId}",
                    RefreshToken = $"mock-refresh-{user.UserId}",
                    User = user,
                }
            )
        );
    }

    public Task RegisterAsync(RegisterRequest request)
    {
        if (store.Users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Email đã được sử dụng.");

        store.PendingOtps[request.Email] = "123456";
        return Task.CompletedTask;
    }

    public Task VerifyEmailAsync(VerifyEmailRequest request)
    {
        if (!store.PendingOtps.TryGetValue(request.Email, out var otp) || otp != request.Otp)
            throw new InvalidOperationException("Mã OTP không hợp lệ.");

        store.PendingOtps.Remove(request.Email);
        var id = store.NextUserId();
        store.Users.Add(
            new UserDto
            {
                UserId = id,
                FullName = "Khách hàng mới",
                Email = request.Email,
                Role = "Customer",
                MembershipTier = "Bronze",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            }
        );
        store.Passwords[request.Email] = "Customer@123";
        return Task.CompletedTask;
    }

    public Task LogoutAsync() => Task.CompletedTask;

    public Task<UserDto?> GetCurrentUserAsync()
    {
        var user = GetAuthenticatedUser();
        return Task.FromResult<UserDto?>(user);
    }

    public Task<UserDto> UpdateProfileAsync(UpdateProfileViewModel request)
    {
        var user =
            GetAuthenticatedUser()
            ?? throw new InvalidOperationException("Không tìm thấy người dùng hiện tại.");

        user.FullName = request.FullName;
        user.Phone = request.Phone;
        user.AvatarUrl = request.AvatarUrl;
        user.DateOfBirth = request.DateOfBirth;
        user.Gender = request.Gender;
        user.SkillLevel = request.SkillLevel;

        return Task.FromResult(user);
    }

    public Task ChangePasswordAsync(ChangePasswordViewModel request)
    {
        var user =
            GetAuthenticatedUser()
            ?? throw new InvalidOperationException("Không tìm thấy người dùng hiện tại.");
        if (
            !store.Passwords.TryGetValue(user.Email, out var currentPassword)
            || currentPassword != request.OldPassword
        )
            throw new InvalidOperationException("Mật khẩu hiện tại không chính xác.");

        store.Passwords[user.Email] = request.NewPassword;
        return Task.CompletedTask;
    }

    public Task<AuthLoginResult> GoogleLoginAsync(string idToken)
    {
        var mockEmail = "google-customer@sportcourtms.vn";
        var user = store.Users.FirstOrDefault(u =>
            u.Email.Equals(mockEmail, StringComparison.OrdinalIgnoreCase)
        );

        if (user == null)
        {
            user = new UserDto
            {
                UserId = store.NextUserId(),
                FullName = "Google Customer (Mock)",
                Email = mockEmail,
                Role = "Customer",
                MembershipTier = "Bronze",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            store.Users.Add(user);
        }

        return Task.FromResult(
            AuthLoginResult.Ok(
                new AuthResponse
                {
                    AccessToken = $"mock-google-token-{user.UserId}",
                    RefreshToken = $"mock-google-refresh-{user.UserId}",
                    User = user,
                }
            )
        );
    }

    private UserDto? GetAuthenticatedUser()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return null;

        var email = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return null;

        return store.Users.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)
        );
    }
}
