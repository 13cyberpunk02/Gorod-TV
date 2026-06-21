
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorodTV.Core.Models;
using GorodTV.Core.Services;
using System.Collections.ObjectModel;

namespace GorodTV.Core.ViewModels;

public partial class ChannelListViewModel : ObservableObject, IQueryAttributable
{
    private readonly IGorodTvService _channels;
    private readonly IDialogService _dialogs;
    private readonly IAppSettings _settings;
    private CancellationTokenSource? _epgCts;

    public ChannelListViewModel(IGorodTvService channels, IDialogService dialogs, IAppSettings settings)
    {
        _channels = channels;
        _dialogs = dialogs;
        _settings = settings;

        // восстановить сохранённый вид (список/плитки) из настроек
        _isGrid = _settings.ChannelView == ChannelViewMode.Grid;
    }

    [ObservableProperty]
    private string _categoryTitle = "Каналы";

    public string CategoryId { get; private set; } = "all";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    // ===== Переключатель вида список/плитки =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsList))]
    [NotifyPropertyChangedFor(nameof(ViewToggleGlyph))]
    private bool _isGrid;

    public bool IsList => !IsGrid;

    // иконка ПРОТИВОПОЛОЖНОГО вида — то, на что переключимся
    public string ViewToggleGlyph => IsGrid ? "\ue8ef" : "\ue9b0"; // view_list : grid_view

    [RelayCommand]
    private void ToggleView()
    {
        IsGrid = !IsGrid;
        _settings.ChannelView = IsGrid ? ChannelViewMode.Grid : ChannelViewMode.List;
    }

    public ObservableCollection<ChannelItem> Channels { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("category", out var id))
            CategoryId = Uri.UnescapeDataString(id.ToString() ?? "all");
        if (query.TryGetValue("title", out var title))
            CategoryTitle = Uri.UnescapeDataString(title.ToString() ?? "Каналы");

        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;
            var items = await _channels.GetChannelsAsync(CategoryId);
            Channels.Clear();
            foreach (var c in items)
                Channels.Add(c);

            StartLazyEpg(items);
        }
        catch (Exception)
        {
            await _dialogs.AlertAsync("Не удалось загрузить",
                "Список каналов временно недоступен. Попробуйте позже.",
                AlertKind.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CancelEpg() => _epgCts?.Cancel();

    private void StartLazyEpg(IReadOnlyList<ChannelItem> channels)
    {
        _epgCts?.Cancel();              // отменить прошлую загрузку
        _epgCts = new CancellationTokenSource();
        var ct = _epgCts.Token;
        _ = LoadEpgLazyAsync(channels, ct);
    }

    private async Task LoadEpgLazyAsync(IReadOnlyList<ChannelItem> channels, CancellationToken ct)
    {
        // дать списку отрисоваться
        await Task.Delay(400, ct).ConfigureAwait(false);

        long now;
        try { now = await _channels.GetServerTimeAsync(ct); }
        catch { now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }

        const int batch = 4;            // по 4 канала за раз
        for (int i = 0; i < channels.Count; i++)
        {
            if (ct.IsCancellationRequested) return;   // ушли со страницы — стоп

            var ch = channels[i];
            try
            {
                var epg = await _channels.GetEpgForDayAsync(ch.Id.ToString(), now - 86400, ct);
                EpgItem? current = null;
                foreach (var e in epg)
                    if (e.StartTimeUnix <= now) current = e; else break;

                if (current is not null)
                {
                    // обновляем в UI-потоке (ObservableProperty)
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ch.CurrentProgram = current.Caption;
                        ch.IsLive = true;
                    });
                }
            }
            catch (OperationCanceledException) { return; }
            catch { /* один канал не критичен */ }

            // пауза между батчами — не душим сеть и CPU
            if ((i + 1) % batch == 0)
                await Task.Delay(150, ct).ConfigureAwait(false);
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(ChannelItem channel)
    {
        channel.IsFavorite = _channels.ToggleFavorite(channel.Id);  // пишет в Preferences
        await _dialogs.ToastAsync(channel.IsFavorite
            ? $"«{channel.Name}» добавлен в избранное"
            : $"«{channel.Name}» убран из избранного");
    }

    [RelayCommand]
    private Task OpenChannelAsync(ChannelItem channel)
        => Shell.Current.GoToAsync(
            $"player?id={channel.Id}&name={Uri.EscapeDataString(channel.Name)}");

    [RelayCommand]
    private Task BackAsync() => Shell.Current.GoToAsync("..");

    [RelayCommand]
    private Task SearchAsync() => Task.CompletedTask;
}