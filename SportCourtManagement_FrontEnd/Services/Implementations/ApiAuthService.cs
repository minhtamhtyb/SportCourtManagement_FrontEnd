using SportCourtManagement_FrontEnd.Models.DTOs;
using SportCourtManagement_FrontEnd.Services.Api;
using SportCourtManagement_FrontEnd.Services.Interfaces;

namespace SportCourtManagement_FrontEnd.Services.Implementations;

public class ApiAuthService(ApiClient api) : IAuthService
{
    public async Task<AuthLoginResult> LoginAsync(LoginRequest request)
    {
        try
        {
            var (data, error, statusCode) = await api.PostForResultAsync<AuthResponse>("api/auth/login", request);

            if (data is not null)
                return AuthLoginResult.Ok(data);

            if (statusCode == 403 && error?.Contains("xác thực", StringComparison.OrdinalIgnoreCase) == true)
                return AuthLoginResult.Fail(error, requiresVerification: true);

            return AuthLoginResult.Fail(error ?? "Email hoặc mật khẩu không đúng.");
        }
        catch (InvalidOperationException ex)
        {
            return AuthLoginResult.Fail(ex.Message);
        }
    }

    public Task RegisterAsync(RegisterRequest request) =>
        api.PostOrThrowAsync("api/auth/register", request);

    public Task VerifyEmailAsync(VerifyEmailRequest request) =>
        api.PostOrThrowAsync("api/auth/verify-email", request);

    public async Task LogoutAsync()
    {
        try
        {
            await api.PostOrThrowAsync("api/auth/logout");
        }
        catch
        {
            // MVC vẫn xóa cookie/session phía client
        }
    }

    public Task<UserDto?> GetCurrentUserAsync() =>
        api.GetDataAsync<UserDto>("api/auth/me");
}
