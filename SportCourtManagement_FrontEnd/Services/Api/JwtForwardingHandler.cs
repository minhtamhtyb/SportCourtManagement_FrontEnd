using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

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
            System.Console.WriteLine($"[JwtForwardingHandler] Resolving token for request {request.RequestUri}. Token exists: {!string.IsNullOrWhiteSpace(token)}");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                System.Console.WriteLine($"[JwtForwardingHandler] Authorization header set to: Bearer {(token.Length > 15 ? token.Substring(0, 15) : token)}...");
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
                        System.Console.WriteLine($"[JwtForwardingHandler] JWT Decoded Payload JSON: {json}");
                    }
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"[JwtForwardingHandler] Failed to custom decode JWT: {ex.Message}");
                }
            }
            else
            {
                System.Console.WriteLine($"[JwtForwardingHandler] Token is empty or null!");
            }
        }
        else
        {
            System.Console.WriteLine($"[JwtForwardingHandler] HttpContext is null!");
        }

        return await base.SendAsync(request, cancellationToken);
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
}
