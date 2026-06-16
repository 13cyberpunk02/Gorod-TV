
namespace GorodTV.Core.Services;

public enum ChannelViewMode { List, Grid }

public interface IAppSettings
{
    ChannelViewMode ChannelView { get; set; }
}

public class AppSettings : IAppSettings
{
    private const string ChannelViewKey = "channel_view_mode";

    public ChannelViewMode ChannelView
    {
        get => (ChannelViewMode)Preferences.Default.Get(ChannelViewKey, (int)ChannelViewMode.List);
        set => Preferences.Default.Set(ChannelViewKey, (int)value);
    }
}