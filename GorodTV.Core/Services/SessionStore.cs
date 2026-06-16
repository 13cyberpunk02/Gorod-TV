namespace GorodTV.Core.Services;


/// <summary>Хранит sessionId и учётные данные между запусками (Preferences).</summary>
public interface ISessionStore
{
    string? SessionId { get; set; }
    string? Login { get; set; }
    string? Password { get; set; }
    bool HasSession { get; }
    void Clear();
}

public class SessionStore : ISessionStore
{
    private const string KeySession = "session_id";
    private const string KeyLogin = "auth_login";
    private const string KeyPassword = "auth_password";

    public string? SessionId
    {
        get { var v = Preferences.Default.Get(KeySession, string.Empty); return string.IsNullOrEmpty(v) ? null : v; }
        set => Set(KeySession, value);
    }

    public string? Login
    {
        get { var v = Preferences.Default.Get(KeyLogin, string.Empty); return string.IsNullOrEmpty(v) ? null : v; }
        set => Set(KeyLogin, value);
    }

    public string? Password
    {
        get { var v = Preferences.Default.Get(KeyPassword, string.Empty); return string.IsNullOrEmpty(v) ? null : v; }
        set => Set(KeyPassword, value);
    }

    public bool HasSession => !string.IsNullOrWhiteSpace(SessionId);

    public void Clear()
    {
        Preferences.Default.Remove(KeySession);
        Preferences.Default.Remove(KeyLogin);
        Preferences.Default.Remove(KeyPassword);
    }

    private static void Set(string key, string? value)
    {
        if (string.IsNullOrEmpty(value)) Preferences.Default.Remove(key);
        else Preferences.Default.Set(key, value);
    }
}
