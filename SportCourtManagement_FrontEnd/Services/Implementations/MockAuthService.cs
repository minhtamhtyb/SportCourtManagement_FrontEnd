using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class MockAuthService(MockDataStore store) : IAuthService
{
    public Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = store.Users.FirstOrDefault(u =>
            u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

        if (user == null || !store.Passwords.TryGetValue(request.Email, out var pwd) || pwd != request.Password)
            return Task.FromResult<AuthResponse?>(null);

        if (!user.IsActive)
            return Task.FromResult<AuthResponse?>(null);

        return Task.FromResult<AuthResponse?>(new AuthResponse
        {
            AccessToken = $"mock-token-{user.UserId}",
            RefreshToken = $"mock-refresh-{user.UserId}",
            User = user
        });
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
        store.Users.Add(new UserDto
        {
            UserId = id,
            FullName = "Khách hàng mới",
            Email = request.Email,
            Role = "Customer",
            MembershipTier = "Bronze",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        store.Passwords[request.Email] = "Customer@123";
        return Task.CompletedTask;
    }

    public Task LogoutAsync() => Task.CompletedTask;

    public Task<UserDto?> GetCurrentUserAsync() => Task.FromResult<UserDto?>(null);
}
