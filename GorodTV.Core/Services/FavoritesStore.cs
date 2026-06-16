namespace GorodTV.Core.Services;


/// <summary>Хранит id избранных каналов локально (Preferences).</summary>
public interface IFavoritesStore
{
    IReadOnlyCollection<int> All { get; }
    bool Contains(int channelId);
    void Add(int channelId);
    void Remove(int channelId);
    bool Toggle(int channelId);   // вернёт новое состояние (true — теперь в избранном)
}

public class FavoritesStore : IFavoritesStore
{
    private const string Key = "favorite_channels";
    private readonly HashSet<int> _ids;

    public FavoritesStore()
    {
        _ids = Load();
    }

    public IReadOnlyCollection<int> All => _ids;

    public bool Contains(int channelId) => _ids.Contains(channelId);

    public void Add(int channelId)
    {
        if (_ids.Add(channelId)) Save();
    }

    public void Remove(int channelId)
    {
        if (_ids.Remove(channelId)) Save();
    }

    public bool Toggle(int channelId)
    {
        bool nowFav;
        if (_ids.Contains(channelId)) { _ids.Remove(channelId); nowFav = false; }
        else { _ids.Add(channelId); nowFav = true; }
        Save();
        return nowFav;
    }

    private static HashSet<int> Load()
    {
        var raw = Preferences.Default.Get(Key, string.Empty);
        if (string.IsNullOrEmpty(raw)) return new HashSet<int>();
        var set = new HashSet<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
            if (int.TryParse(part, out var id)) set.Add(id);
        return set;
    }

    private void Save()
        => Preferences.Default.Set(Key, string.Join(',', _ids));
}

