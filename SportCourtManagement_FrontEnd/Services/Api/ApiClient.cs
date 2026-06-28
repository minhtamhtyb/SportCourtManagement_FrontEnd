using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SportCourtManagement_FrontEnd.Models.Api;

namespace SportCourtManagement_FrontEnd.Services.Api;

public class ApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> GetDataAsync<T>(string path, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        var response = await SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return default;

        var (wrapper, error) = await ReadWrapperAsync<T>(response, ct);
        if (error is not null || wrapper is not { Success: true })
            return default;

        return wrapper.Data;
    }

    public async Task<(T? Data, string? ErrorMessage, int StatusCode)> PostForResultAsync<T>(
        string path, object? body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        var response = await SendAsync(request, ct);
        var (wrapper, error) = await ReadWrapperAsync<T>(response, ct);

        if (error is not null)
            return (default, error, (int)response.StatusCode);

        if (wrapper is { Success: true })
            return (wrapper.Data, null, wrapper.StatusCode > 0 ? wrapper.StatusCode : (int)response.StatusCode);

        var message = wrapper?.Message ?? $"API trả về lỗi ({(int)response.StatusCode}).";
        return (default, message, wrapper?.StatusCode > 0 ? wrapper.StatusCode : (int)response.StatusCode);
    }

    public async Task<T?> PostDataAsync<T>(string path, object? body, CancellationToken ct = default)
    {
        var (data, error, _) = await PostForResultAsync<T>(path, body, ct);
        if (error is not null)
            throw new InvalidOperationException(error);
        return data;
    }

    public async Task<T?> PostMultipartDataAsync<T>(string path, IFormFile file, CancellationToken ct = default)
    {
        using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", file.FileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        var response = await SendAsync(request, ct);
        var (wrapper, error) = await ReadWrapperAsync<T>(response, ct);

        if (error is not null)
            throw new InvalidOperationException(error);

        if (wrapper is { Success: true })
            return wrapper.Data;

        throw new InvalidOperationException(wrapper?.Message ?? $"Upload thất bại ({(int)response.StatusCode}).");
    }

    public async Task PostOrThrowAsync(string path, object? body = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        var response = await SendAsync(request, ct);
        var (wrapper, error) = await ReadWrapperAsync<object>(response, ct);
        if (error is not null)
            throw new InvalidOperationException(error);

        if (response.IsSuccessStatusCode && wrapper is { Success: true })
            return;

        throw new InvalidOperationException(wrapper?.Message ?? $"API trả về lỗi ({(int)response.StatusCode}).");
    }

    public Task PostAsync(string path, object? body = null, CancellationToken ct = default) =>
        PostOrThrowAsync(path, body, ct);

    public async Task<T?> PutDataAsync<T>(string path, object? body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        var response = await SendAsync(request, ct);
        return await ReadDataAsync<T>(response, ct);
    }

    public async Task<T?> PatchDataAsync<T>(string path, object? body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        var response = await SendAsync(request, ct);
        return await ReadDataAsync<T>(response, ct);
    }

    public async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        var response = await SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
            return;

        var (wrapper, error) = await ReadWrapperAsync<object>(response, ct);
        throw new InvalidOperationException(error ?? wrapper?.Message ?? $"API trả về lỗi ({(int)response.StatusCode}).");
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await ApplyAuthHeaderAsync(request, ct);
        return await http.SendAsync(request, ct);
    }

    private async Task ApplyAuthHeaderAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        var token = await JwtForwardingHandler.ResolveAccessTokenAsync(httpContext, ct);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return;
        }

        // Login/register endpoints do not need a token.
        if (IsAnonymousAuthRequest(request))
            return;

        throw new InvalidOperationException(
            "Chưa có token API trong phiên làm việc. Vui lòng đăng xuất và đăng nhập lại.");
    }

    private static bool IsAnonymousAuthRequest(HttpRequestMessage request)
    {
        var uri = request.RequestUri;
        if (uri is null)
            return false;

        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        return path.Contains("api/auth/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<T?> ReadDataAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var (wrapper, error) = await ReadWrapperAsync<T>(response, ct);
        if (error is not null)
            throw new InvalidOperationException(error);

        if (response.IsSuccessStatusCode && wrapper is { Success: true })
            return wrapper.Data;

        throw new InvalidOperationException(wrapper?.Message ?? $"API trả về lỗi ({(int)response.StatusCode}).");
    }

    private static async Task<(ApiResponse<T>? Wrapper, string? Error)> ReadWrapperAsync<T>(
        HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
        {
            var msg = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Backend từ chối yêu cầu (401). Token API không hợp lệ hoặc đã hết hạn — hãy đăng xuất và đăng nhập lại."
                : $"API trả về phản hồi rỗng (HTTP {(int)response.StatusCode}). Kiểm tra Backend đang chạy tại port 5211.";
            return (null, msg);
        }

        try
        {
            var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(body, JsonOptions);
            return (wrapper, null);
        }
        catch (JsonException)
        {
            return (null, $"API trả về dữ liệu không hợp lệ (HTTP {(int)response.StatusCode}). Kiểm tra Backend đang chạy.");
        }
    }
}
