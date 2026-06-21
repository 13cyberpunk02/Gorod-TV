
using GorodTV.Core.Models;
using GorodTV.Core.Models.DTOs.Request;
using GorodTV.Core.Models.DTOs.Response;

namespace GorodTV.Core.Services;

public enum AuthResult { Success, InvalidCredentials, NetworkError }

/// <summary>
/// Высокоуровневый сервис: авторизация, переиспользование сессии,
/// автоматический перелогин при протухшем sessionId, маппинг в доменные модели.
/// </summary>
public interface IGorodTvService
{
    Task<AuthResult> LoginAsync(string contract, string password, CancellationToken ct = default);
    Task<bool> TryRestoreSessionAsync(CancellationToken ct = default);
    void Logout();

    Task<IReadOnlyList<CategoryItem>> GetCategoriesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ChannelItem>> GetChannelsAsync(string? categoryId = null, CancellationToken ct = default);
    Task<IReadOnlyList<EpgItem>> GetEpgAsync(string channelEpgId, CancellationToken ct = default);
    Task<IReadOnlyList<EpgItem>> GetEpgRangeAsync(string channelEpgId, int daysBack, CancellationToken ct = default);
    Task<long> GetServerTimeAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ChannelItem>> GetFavoritesAsync(CancellationToken ct = default);
    bool ToggleFavorite(int channelId);
    bool IsFavorite(int channelId);
    Task<IReadOnlyList<EpgItem>> GetEpgForDayAsync(string channelEpgId, long dayStartUnix, CancellationToken ct = default);
}

public class GorodTvService : IGorodTvService
{
    private readonly IApiClient _api;
    private readonly ISessionStore _session;
    private readonly IFavoritesStore _favorites;
    private IReadOnlyList<CategoryWithChannelsDto>? _catChanCache;

    // кэш «сырых» каналов — категории и фильтры считаются из него
    private IReadOnlyList<ChannelDto>? _channelsCache;
    private IReadOnlyList<CategoryDto>? _categoriesCache;

    public GorodTvService(IApiClient api, ISessionStore session, IFavoritesStore favorites)
    {
        _api = api;
        _session = session;
        _favorites = favorites;
    }

    public async Task<AuthResult> LoginAsync(string contract, string password, CancellationToken ct = default)
    {
        try
        {
            var sid = await _api.AuthAsync(new LoginRequest(contract, password), ct);
            if (string.IsNullOrWhiteSpace(sid))
                return AuthResult.InvalidCredentials;

            _session.SessionId = sid;
            _session.Login = contract;
            _session.Password = password;
            return AuthResult.Success;
        }
        catch
        {
            return AuthResult.NetworkError;
        }
    }

    /// <summary>
    /// На старте: sessionId долгоживущий, поэтому если он есть — сразу пускаем внутрь
    /// БЕЗ сетевой проверки (быстро и не зависит от сети на старте). Если сервер
    /// сессию не примет (перезапуск) — первый же запрос вернёт ошибку и WithRetry
    /// автоматически перелогинится по сохранённым логину/паролю.
    /// </summary>
    public Task<bool> TryRestoreSessionAsync(CancellationToken ct = default)
        => Task.FromResult(_session.HasSession);

    public void Logout()
    {
        _session.Clear();
        _catChanCache = null;
    }

    public async Task<IReadOnlyList<CategoryItem>> GetCategoriesAsync(CancellationToken ct = default)
    {
        var data = await EnsureCatChanAsync(ct);
        var result = new List<CategoryItem>(data.Count);
        for (int i = 0; i < data.Count; i++)
            result.Add(ChannelMapper.BuildCategoryFromCatChan(data[i], i));
        return result;
    }


    public async Task<IReadOnlyList<ChannelItem>> GetChannelsAsync(
        string? categoryId = null, CancellationToken ct = default)
    {
        var data = await EnsureCatChanAsync(ct);
        IEnumerable<ChannelInCategoryDto> channels =
            string.IsNullOrEmpty(categoryId) || categoryId == "all"
                ? data.SelectMany(c => c.ChannelsList ?? Enumerable.Empty<ChannelInCategoryDto>())
                : (data.FirstOrDefault(c => c.Id == categoryId)?.ChannelsList
                   ?? Enumerable.Empty<ChannelInCategoryDto>());
        return [.. channels.Select(ChannelMapper.BuildChannelFromCatChan)];
    }

    public Task<IReadOnlyList<EpgItem>> GetEpgAsync(string channelEpgId, CancellationToken ct = default)
        => GetEpgRangeAsync(channelEpgId, 0, ct);

    public Task<long> GetServerTimeAsync(CancellationToken ct = default)
        => _api.GetServerUnixTimeAsync(ct);

    public bool IsFavorite(int channelId) => _favorites.Contains(channelId);

    public bool ToggleFavorite(int channelId) => _favorites.Toggle(channelId);

    public async Task<IReadOnlyList<ChannelItem>> GetFavoritesAsync(CancellationToken ct = default)
    {
        var all = await EnsureChannelsAsync(ct);
        var favIds = _favorites.All;
        var list = ChannelMapper.BuildChannels(all)
            .Where(c => favIds.Contains(c.Id))
            .ToList();
        foreach (var c in list) c.IsFavorite = true;
        return list;
    }

    public async Task<IReadOnlyList<EpgItem>> GetEpgForDayAsync(string channelEpgId, long dayStartUnix, CancellationToken ct = default)
    {
        RequireSession();
        var resp = await WithRetry(s => _api.GetEpgAsync(channelEpgId, dayStartUnix.ToString(), s, ct), ct);
        if (resp?.Epg is null) return [];

        return resp.Epg
            .Select(e => new EpgItem
            {
                Caption = e.Caption,
                Description = e.Description ?? "",
                StartTimeUnix = long.TryParse(e.StartTime, out var t) ? t : 0,
                HasRecord = e.Record,
            })
            .Where(e => e.StartTimeUnix > 0)
            .OrderBy(e => e.StartTimeUnix)
            .ToList();
    }

    public async Task<IReadOnlyList<EpgItem>> GetEpgRangeAsync(string channelEpgId, int daysBack, CancellationToken ct = default)
    {
        var sid = RequireSession();
        var now = await _api.GetServerUnixTimeAsync(ct);

        // тянем за каждый день от (сегодня - daysBack) до сегодня; allday=true даёт весь день
        var byStart = new Dictionary<long, EpgItem>();
        for (int d = daysBack; d >= 0; d--)
        {
            long dayStart = now - (long)d * 86400;
            var resp = await WithRetry(s => _api.GetEpgAsync(channelEpgId, dayStart.ToString(), s, ct), ct);
            if (resp?.Epg is null) continue;

            foreach (var e in resp.Epg)
            {
                long start = long.TryParse(e.StartTime, out var t) ? t : 0;
                if (start == 0) continue;
                byStart[start] = new EpgItem
                {
                    Caption = e.Caption,
                    Description = e.Description ?? "",
                    StartTimeUnix = start,
                    HasRecord = e.Record,
                };
            }
        }

        return byStart.Values.OrderBy(e => e.StartTimeUnix).ToList();
    }

    // ===== внутреннее =====

    private async Task<IReadOnlyList<CategoryDto>> EnsureCategoriesAsync(CancellationToken ct)
    {
        if (_categoriesCache is not null) return _categoriesCache;
        var resp = await WithRetry(s => _api.GetCategoriesAsync(s, ct), ct);
        return _categoriesCache = resp?.Categories ?? Array.Empty<CategoryDto>();
    }

    private async Task<IReadOnlyList<ChannelDto>> EnsureChannelsAsync(CancellationToken ct)
    {
        if (_channelsCache is not null) return _channelsCache;
        var resp = await WithRetry(s => _api.GetChannelsAsync(s, ct), ct);
        return _channelsCache = resp?.Channels ?? Array.Empty<ChannelDto>();
    }

    /// <summary>Выполняет запрос; при пустом/неуспешном ответе один раз перелогинивается и повторяет.</summary>
    private async Task<T?> WithRetry<T>(Func<string, Task<T?>> call, CancellationToken ct) where T : class
    {
        var sid = RequireSession();
        try
        {
            var result = await call(sid);
            if (result is not null) return result;
        }
        catch { /* упадём в перелогин ниже */ }

        // повтор после перелогина
        if (!string.IsNullOrEmpty(_session.Login) && !string.IsNullOrEmpty(_session.Password))
        {
            if (await LoginAsync(_session.Login!, _session.Password!, ct) == AuthResult.Success)
                return await call(_session.SessionId!);
        }
        return null;
    }

    private string RequireSession()
        => _session.SessionId ?? throw new InvalidOperationException("Нет активной сессии. Требуется вход.");

    private async Task<IReadOnlyList<CategoryWithChannelsDto>> EnsureCatChanAsync(CancellationToken ct)
    {
        if (_catChanCache is not null) return _catChanCache;
        var resp = await WithRetry(s => _api.GetCategoriesAndChannelsAsync(s, _session.Login!, _session.Password!, ct), ct);
        return _catChanCache = resp?.CategoriesAndChannels
                               ?? Array.Empty<CategoryWithChannelsDto>();
    }
}
