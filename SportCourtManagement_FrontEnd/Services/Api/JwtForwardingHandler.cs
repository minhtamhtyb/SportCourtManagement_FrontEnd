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
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    internal static async Task<string?> ResolveAccessTokenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (httpContext.Session.IsAvailable)
        {
            await httpContext.Session.LoadAsync(cancellationToken);
            var sessionToken = httpContext.Session.GetString(SessionTokenKey);
            if (!string.IsNullOrWhiteSpace(sessionToken))
                return sessionToken;
        }

        var authToken = await httpContext.GetTokenAsync("access_token");
        if (!string.IsNullOrWhiteSpace(authToken))
            return authToken;

        return httpContext.User.FindFirst(AccessTokenClaimType)?.Value;
    }
}
