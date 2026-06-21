
using CommunityToolkit.Mvvm.ComponentModel;

namespace GorodTV.Core.Models;


public partial class CategoryItem : ObservableObject
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string? IconUrl { get; init; }

    public Color Tint { get; init; } = Color.FromArgb("#E4EDFC");
    public Color IconColor { get; init; } = Color.FromArgb("#1B66E5");
    public string FallbackGlyph { get; init; } = "\ue72c";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountText))]
    private int _count;

    public bool HasIcon => !string.IsNullOrWhiteSpace(IconUrl);

    public string CountText
    {
        get
        {
            var n = Math.Abs(Count) % 100;
            var n1 = n % 10;
            string word = n is >= 11 and <= 14 ? "каналов"
                        : n1 == 1 ? "канал"
                        : n1 is >= 2 and <= 4 ? "канала"
                        : "каналов";
            return $"{Count} {word}";
        }
    }
}

public partial class ChannelItem : ObservableObject
{
    private static readonly Color FavOff = Color.FromArgb("#C7CEDA");
    private static readonly Color FavOn = Color.FromArgb("#E5342B");

    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string CategoryId { get; init; }
    public string EpgId { get; init; } = "";
    public string? IconUrl { get; init; }
    public string Link { get; init; } = "";

    public required string Abbrev { get; init; }
    public required Color TileColor { get; init; }

    public bool NoIcon => !HasIcon;

    public bool HasIcon => !string.IsNullOrWhiteSpace(IconUrl);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEpg))]
    private string? _currentProgram;

    [ObservableProperty] private double _progress;
    [ObservableProperty] private bool _isLive;

    public bool HasEpg => !string.IsNullOrWhiteSpace(CurrentProgram);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteColor))]
    private bool _isFavorite;

    public Color FavoriteColor => IsFavorite ? FavOn : FavOff;
}

/// <summary>Одна передача EPG (для списка на странице плеера).</summary>
public partial class EpgItem : ObservableObject
{
    public required string Caption { get; init; }
    public string Description { get; init; } = "";
    public long StartTimeUnix { get; init; }
    public bool HasRecord { get; init; }   // record=true — есть запись

    public DateTime StartLocal =>
        DateTimeOffset.FromUnixTimeSeconds(StartTimeUnix).LocalDateTime;

    public string StartTimeText => StartLocal.ToString("HH:mm");

    // ===== состояние для UI (выставляется при загрузке списка) =====

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptionColor))]
    [NotifyPropertyChangedFor(nameof(TimeColor))]
    private bool _isPast;        // передача уже прошла (или идёт) — кликабельна

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptionColor))]
    private bool _isCurrent;     // идёт прямо сейчас — подсветка

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusGlyph))]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    private bool _isFuture;      // ещё не началась

    // выбрана для воспроизведения (играет именно она)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RowBg))]
    [NotifyPropertyChangedFor(nameof(CaptionColor))]
    private bool _isSelected;

    // фон строки: выбранная — голубая подсветка
    public Color RowBg => IsSelected ? Color.FromArgb("#E6F1FB") : Colors.Transparent;

    // прошедшие/текущая — обычный текст; будущие — серый
    public Color CaptionColor => (IsCurrent || IsSelected)
        ? Color.FromArgb("#1B66E5")
        : IsPast ? Color.FromArgb("#15191E") : Color.FromArgb("#B0B8C4");

    public Color TimeColor => IsPast ? Color.FromArgb("#8A94A6") : Color.FromArgb("#B0B8C4");

    // можно открыть архив: прошедшие и текущая (будущие — нет)
    public bool CanPlay => IsPast;

    // значок справа: текущая — эфир, прошедшая — play, будущая — часы
    public string StatusGlyph => IsCurrent ? ""   // live_tv
                               : IsPast ? ""   // play_arrow
                               : "";  // schedule

    public Color StatusColor => IsFuture
        ? Color.FromArgb("#B0B8C4")
        : Color.FromArgb("#1B66E5");
}

/// <summary>День в ленте архива (Сегодня, Вчера, дата...).</summary>
public partial class DayTab : ObservableObject
{
    public required DateTime Date { get; init; }   // локальная дата (полночь)
    public required string Title { get; init; }    // «Сегодня», «Вчера», «Сб 14»
    public int DaysBack { get; init; }             // 0 = сегодня, 1 = вчера...

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BgColor))]
    [NotifyPropertyChangedFor(nameof(TextColor))]
    private bool _isSelected;

    public Color BgColor => IsSelected ? Color.FromArgb("#1B66E5") : Color.FromArgb("#FFFFFF");
    public Color TextColor => IsSelected ? Colors.White : Color.FromArgb("#15191E");
}