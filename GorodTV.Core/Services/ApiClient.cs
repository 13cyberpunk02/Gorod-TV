using GorodTV.Core.Models.DTOs.Request;
using GorodTV.Core.Models.DTOs.Response;
using System.Net.Http.Json;
using System.Text.Json;

namespace GorodTV.Core.Services;

/// <summary>Низкоуровневый клиент: строит запрос, шлёт, десериализует.</summary>
public interface IApiClient
{
    Task<string?> AuthAsync(LoginRequest request, CancellationToken ct = default);
    Task<bool> IsSessionValidAsync(string sessionId, CancellationToken ct = default);
    Task<CategoriesAndChannelsResponse?> GetCategoriesAndChannelsAsync(
            string sessionId, string username, string password, CancellationToken ct = default);
    Task<CategoriesResponse?> GetCategoriesAsync(string sessionId, CancellationToken ct = default);
    Task<ChannelsResponse?> GetChannelsAsync(string sessionId, CancellationToken ct = default);
    Task<EpgResponse?> GetEpgAsync(string channelId, string startTime, string sessionId, CancellationToken ct = default);
    Task<long> GetServerUnixTimeAsync(CancellationToken ct = default);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(HttpClient http) => _http = http;

    public async Task<string?> AuthAsync(LoginRequest request, CancellationToken ct = default)
    {
        var res = await GetAsync<AuthResponse>(BaseApiRequests.GetAuthRequestString(request), ct);
        return res?.SessionId;
    }

    public async Task<bool> IsSessionValidAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var url = BaseApiRequests.GetIsSessionValidString(sessionId);
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return false;
            var body = await resp.Content.ReadFromJsonAsync<CategoriesResponse>(JsonOpts, ct);
            return body?.Categories is { Count: > 0 };
        }
        catch { return false; }
    }

    public Task<CategoriesResponse?> GetCategoriesAsync(string sessionId, CancellationToken ct = default)
        => GetAsync<CategoriesResponse>(BaseApiRequests.GetCategoryRequestString(sessionId), ct);

    public Task<ChannelsResponse?> GetChannelsAsync(string sessionId, CancellationToken ct = default)
        => GetAsync<ChannelsResponse>(BaseApiRequests.GetChannelsRequestString(sessionId), ct);

    public async Task<EpgResponse?> GetEpgAsync(string channelId, string startTime, string sessionId, CancellationToken ct = default)
    {
        var url = BaseApiRequests.GetEpgRequestString(startTime, channelId, sessionId);
        System.Diagnostics.Debug.WriteLine($"[EPG] GET {url}");
        using var resp = await _http.GetAsync(url, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) return null;
        return JsonSerializer.Deserialize<EpgResponse>(body, JsonOpts);
    }

    public async Task<long> GetServerUnixTimeAsync(CancellationToken ct = default)
    {
        var url = BaseApiRequests.GetUnixTimeRequestString;
        try
        {
            using var resp = await _http.GetAsync(url, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var res = JsonSerializer.Deserialize<UnixTimeResponse>(body, JsonOpts);
            return res?.Value ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        catch
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    public Task<CategoriesAndChannelsResponse?> GetCategoriesAndChannelsAsync(
        string sessionId, string username, string password, CancellationToken ct = default)
    => GetAsync<CategoriesAndChannelsResponse>(
        BaseApiRequests.GetCategoriesAndChannelsRequestString(
            username, password, sessionId), ct);


    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
    }
}