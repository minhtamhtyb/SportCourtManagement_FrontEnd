using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SportCourtManagement_FrontEnd.Services.Api;

public class JwtForwardingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    internal const string AccessTokenClaimType = "jwt_access_token";
    internal const string SessionTokenKey = "access_token";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var token = await ResolveAccessTokenAsync(httpContext, cancellationToken);
            if (!string.IsNullOrWhiteSpace(token))
            {
                if (IsJwtTokenExpired(token))
                {
                    System.Console.WriteLine($"[JwtForwardingHandler] Access token EXPIRED for request {request.RequestUri}! Logging out immediately.");
                    await LogoutUserAsync(httpContext);
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        ReasonPhrase = "Token Expired",
                        Content = new StringContent("Access token expired")
                    };
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && httpContext is not null)
        {
            System.Console.WriteLine($"[JwtForwardingHandler] Backend returned 401 Unauthorized for {request.RequestUri}! Logging out immediately.");
            await LogoutUserAsync(httpContext);
        }

        return response;
    }

    internal static async Task<string?> ResolveAccessTokenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        string? token = null;

        if (httpContext.Session.IsAvailable)
        {
            await httpContext.Session.LoadAsync(cancellationToken);
            token = httpContext.Session.GetString(SessionTokenKey);
            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        token = await httpContext.GetTokenAsync("access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            token = httpContext.User.FindFirst(AccessTokenClaimType)?.Value;
        }

        if (!string.IsNullOrWhiteSpace(token) && httpContext.Session.IsAvailable)
        {
            httpContext.Session.SetString(SessionTokenKey, token);
            await httpContext.Session.CommitAsync(cancellationToken);
        }

        return token;
    }

    public static bool IsJwtTokenExpired(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        try
        {
            var parts = token.Split('.');
            if (parts.Length > 1)
            {
                var payload = parts[1];
                payload = payload.Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                var decodedBytes = System.Convert.FromBase64String(payload);
                var json = System.Text.Encoding.UTF8.GetString(decodedBytes);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var expProp))
                {
                    long exp = expProp.GetInt64();
                    var expTime = DateTimeOffset.FromUnixTimeSeconds(exp);
                    return expTime <= DateTimeOffset.UtcNow.AddSeconds(5);
                }
            }
        }
        catch
        {
            return true;
        }

        return false;
    }

    internal static async Task LogoutUserAsync(HttpContext httpContext)
    {
        try
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (httpContext.Session.IsAvailable)
            {
                httpContext.Session.Remove(SessionTokenKey);
                httpContext.Session.Remove("refresh_token");
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[JwtForwardingHandler] LogoutUserAsync error: {ex.Message}");
        }
    }
}
