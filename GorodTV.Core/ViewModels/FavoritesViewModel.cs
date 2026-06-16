using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorodTV.Core.Models;
using GorodTV.Core.Services;
using System.Collections.ObjectModel;

namespace GorodTV.Core.ViewModels;

public partial class FavoritesViewModel : ObservableObject
{
    private readonly IGorodTvService _tv;
    private readonly IDialogService _dialogs;

    public FavoritesViewModel(IGorodTvService tv, IDialogService dialogs)
    {
        _tv = tv;
        _dialogs = dialogs;
    }

    public ObservableCollection<ChannelItem> Channels { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _loaded;

    // пусто — показываем заглушку «нет избранного»
    public bool IsEmpty => Loaded && Channels.Count == 0;

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var items = await _tv.GetFavoritesAsync();
            Channels.Clear();
            foreach (var c in items) Channels.Add(c);

            // подтянуть текущую передачу для каждого канала
            _ = LoadEpgForChannelsAsync(items);
        }
        catch (Exception)
        {
            await _dialogs.AlertAsync("Не удалось загрузить",
                "Избранные каналы временно недоступны.", AlertKind.Error);
        }
        finally
        {
            IsBusy = false;
            Loaded = true;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    // текущая передача под названием канала
    private async Task LoadEpgForChannelsAsync(IReadOnlyList<ChannelItem> channels)
    {
        long now;
        try { now = await _tv.GetServerTimeAsync(); }
        catch { now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }

        foreach (var ch in channels)
        {
            try
            {
                var epg = await _tv.GetEpgForDayAsync(ch.Id.ToString(),
                    now - 86400); // вчера+, чтобы поймать ночную
                EpgItem? current = null;
                foreach (var e in epg)
                    if (e.StartTimeUnix <= now) current = e; else break;

                if (current is not null)
                {
                    ch.CurrentProgram = current.Caption;
                    ch.IsLive = true;
                }
            }
            catch { /* EPG не критичен для строки */ }
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(ChannelItem channel)
    {
        var nowFav = _tv.ToggleFavorite(channel.Id);
        channel.IsFavorite = nowFav;
        if (!nowFav)
        {
            Channels.Remove(channel);   // убрали из избранного — убираем из списка
            OnPropertyChanged(nameof(IsEmpty));
            await _dialogs.ToastAsync($"«{channel.Name}» убран из избранного");
        }
    }

    [RelayCommand]
    private Task OpenChannelAsync(ChannelItem channel)
        => Shell.Current.GoToAsync(
            $"player?id={channel.Id}&name={Uri.EscapeDataString(channel.Name)}");
}

