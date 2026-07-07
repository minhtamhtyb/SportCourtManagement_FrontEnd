namespace SportCourtManagement_FrontEnd.Models.Auth;

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Customer";
    public string? MembershipTier { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}

public class VerifyEmailRequest
{
    public string Email { get; set; } = "";
    public string Otp { get; set; } = "";
}

public class AuthResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public UserDto User { get; set; } = new();
}

public class AuthLoginResult
{
    public AuthResponse? Response { get; init; }
    public string? ErrorMessage { get; init; }
    public bool RequiresEmailVerification { get; init; }

    public bool Succeeded => Response != null;

    public static AuthLoginResult Ok(AuthResponse response) => new() { Response = response };

    public static AuthLoginResult Fail(string message, bool requiresVerification = false) =>
        new() { ErrorMessage = message, RequiresEmailVerification = requiresVerification };
}
