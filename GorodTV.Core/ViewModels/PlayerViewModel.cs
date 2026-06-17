
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorodTV.Core.Models;
using GorodTV.Core.Services;
using System.Collections.ObjectModel;

namespace GorodTV.Core.ViewModels;

[QueryProperty(nameof(ChannelId), "id")]
[QueryProperty(nameof(ChannelName), "name")]
public partial class PlayerViewModel : ObservableObject
{
    private readonly IGorodTvService _tv;

    public PlayerViewModel(IGorodTvService tv) => _tv = tv;

    // ===== Параметры навигации =====
    private int _channelId;
    public int ChannelId
    {
        get => _channelId;
        set
        {
            _channelId = value;
            OnPropertyChanged(nameof(ChannelNumberText));
            _ = LoadAsync();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Abbrev))]
    private string _channelName = "";

    public string ChannelNumberText => $"Канал {ChannelId}";
    public string Abbrev => MakeAbbrev(ChannelName);

    // ===== Плашка канала =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasIcon))]
    private string? _iconUrl;

    [ObservableProperty] private Color _tileColor = Color.FromArgb("#1B66E5");
    public bool HasIcon => !string.IsNullOrWhiteSpace(IconUrl);

    // ===== Видео =====
    [ObservableProperty]
    private string? _streamUrl;     // HLS .m3u8 — источник для MediaElement

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseGlyph))]
    private bool _isPlaying = true;

    public string PlayPauseGlyph => IsPlaying ? "\ue034" : "\ue037"; // pause : play_arrow

    // команду play/pause обрабатывает code-behind (у MediaElement свой контроллер)
    [RelayCommand]
    private void TogglePlay() => IsPlaying = !IsPlaying;

    // ===== Контролы (автоскрытие) =====
    [ObservableProperty] private bool _controlsVisible = true;

    // полноэкранный режим (альбомная, видео на весь экран)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullscreenGlyph))]
    private bool _isFullscreen;

    public string FullscreenGlyph => IsFullscreen ? "\ue5d1" : "\ue5d0"; // fullscreen_exit : fullscreen

    // событие для code-behind: переключить полный экран (поворот + системные панели)
    public event Action<bool>? FullscreenToggleRequested;

    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        FullscreenToggleRequested?.Invoke(IsFullscreen);
    }

    // вызывается из code-behind при повороте устройства
    public void OnOrientationChanged(bool isLandscape)
    {
        if (isLandscape && !IsFullscreen) { IsFullscreen = true; FullscreenToggleRequested?.Invoke(true); }
        else if (!isLandscape && IsFullscreen) { IsFullscreen = false; FullscreenToggleRequested?.Invoke(false); }
    }

    // режим: true — прямой эфир, false — архивная передача
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsArchive))]
    private bool _isLive = true;

    public bool IsArchive => !IsLive;

    // полоса прокрутки считается из EPG (вся передача), НЕ из Media.Duration —
    // HLS-манифест отдаёт лишь маленькое окно, поэтому Duration недостоверна.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress01))]
    private double _positionSeconds;   // позиция на полосе (сек от начала передачи)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress01))]
    private double _durationSeconds;   // длина передачи (сек)

    // доля 0..1 для Slider (Maximum=1 фиксирован -> нет конфликта диапазонов)
    public double Progress01 => DurationSeconds > 0
        ? Math.Clamp(PositionSeconds / DurationSeconds, 0, 1)
        : 0;
    [ObservableProperty] private string _positionText = "0:00";
    [ObservableProperty] private string _durationText = "0:00";

    // границы открытой архивной передачи (абсолютное unix-время)
    private long _archiveProgramStart;
    private long _archiveProgramEnd;

    // unix-время, с которого СЕЙЧАС открыт поток (старт текущего окна воспроизведения)
    private long _windowStartUnix;        // unix-время, с которого открыли поток
    private EpgItem? _selectedProgram;    // какая передача сейчас играет (для подсветки)

    // накопленные РЕАЛЬНО проигранные секунды от _windowStartUnix.
    // HLS отдаёт узкое окно: Media.Position растёт, потом сбрасывается при обновлении
    // манифеста. Считаем приращения, чтобы позиция росла монотонно и стояла при буферизации.
    private double _playedSeconds;        // сколько проиграно от старта окна (сек)

    // событие для code-behind: переоткрыть поток (Source меняется через биндинг StreamUrl)

    // ===== Перемотка через переоткрытие потока =====

    // абсолютное unix-время момента, который сейчас на экране
    private long PlayingUnix(TimeSpan mediaPosition)
        => _windowStartUnix + (long)mediaPosition.TotalSeconds;

    [RelayCommand]
    private void SeekForward() => SeekRelative(15);

    [RelayCommand]
    private void SeekBackward() => SeekRelative(-15);

    // относительная перемотка: пересчитать целевой unix и переоткрыть поток

    private void SeekRelative(int deltaSec)
    {
        long playing = _windowStartUnix + (long)_playedSeconds;
        long target = playing + deltaSec;

        // не выходим за границы передачи (в архиве)
        if (IsArchive)
            target = Math.Clamp(target, _archiveProgramStart, _archiveProgramEnd - 1);

        OpenStreamAt(target);
    }

    // перемотка полосой: value — секунды от начала передачи
    public void SeekToFraction(double secondsFromStart)
    {
        long target = _archiveProgramStart + (long)secondsFromStart;
        target = Math.Clamp(target, _archiveProgramStart, _archiveProgramEnd - 1);
        OpenStreamAt(target);
    }

    // отмотка прямого эфира назад на 5 минут
    [RelayCommand]
    private void RewindLive()
    {
        long target = _serverNow - 300;
        // считаем "передачей" текущую: границы возьмём из блока «сейчас в эфире»,
        // если их нет — просто час вокруг
        if (_archiveProgramEnd <= _archiveProgramStart)
        {
            _archiveProgramStart = _serverNow - 1800;
            _archiveProgramEnd = _serverNow + 1800;
        }
        IsLive = false;
        OpenStreamAt(target);
    }

    private void SetSelected(EpgItem? program)
    {
        if (_selectedProgram is not null) _selectedProgram.IsSelected = false;
        _selectedProgram = program;
        if (program is not null) program.IsSelected = true;
    }

    // когда архивная передача доиграла до конца — перейти к следующей
    private void AdvanceIfEnded(long playingUnix)
    {
        if (IsLive || _selectedProgram is null) return;
        if (playingUnix < _archiveProgramEnd) return;

        // следующая передача в списке
        var idx = DayEpg.IndexOf(_selectedProgram);
        if (idx >= 0 && idx + 1 < DayEpg.Count)
        {
            var next = DayEpg[idx + 1];
            SetSelected(next);
            _archiveProgramStart = next.StartTimeUnix;
            var nIdx = idx + 1;
            _archiveProgramEnd = nIdx + 1 < DayEpg.Count
                ? DayEpg[nIdx + 1].StartTimeUnix
                : next.StartTimeUnix + 3600;
            // поток уже сам перетёк в следующую — НЕ переоткрываем, только обновляем полосу
            UpdateNowPlayingArchive();
            RecalcBar(playingUnix);
        }
    }

    // переоткрыть поток с конкретного unix-времени
    private void OpenStreamAt(long unixTime)
    {
        _windowStartUnix = unixTime;
        _playedSeconds = 0;
        _haveBaseOffset = false;   // первый тик нового потока задаст точку отсчёта

        // СРАЗУ сбросить полосу в начало (до прихода тиков нового потока),
        // иначе ползунок на UI «залипает» на старой позиции.
        // Порядок важен: сначала Value=0, потом новый Maximum — иначе Slider
        // клампит значение по старому Maximum и визуально не сдвигается.
        PositionSeconds = 0;
        DurationSeconds = Math.Max(1, _archiveProgramEnd - _archiveProgramStart);
        PositionText = "0:00";
        DurationText = Format(TimeSpan.FromSeconds(DurationSeconds));

        StreamUrl = StreamUrlBuilder.Build(_rawLink, unixTime);
    }

    // вызывается из code-behind при тике позиции MediaElement
    // базовое смещение позиции нового потока (первый raw после переоткрытия)
    private double _baseRawOffset;
    private bool _haveBaseOffset;

    public void UpdateProgress(TimeSpan position, TimeSpan duration)
    {
        if (IsLive) return;

        double raw = position.TotalSeconds;

        // первый тик после переоткрытия задаёт точку отсчёта: какой бы raw ни был
        // (HLS-архив может стартовать не с нуля), считаем его «нулём» этого окна.
        if (!_haveBaseOffset)
        {
            _baseRawOffset = raw;
            _haveBaseOffset = true;
        }

        // сколько проиграно от начала окна = текущий raw минус базовое смещение.
        // при обновлении HLS-окна raw может сброситься/прыгнуть — тогда
        // переустанавливаем базу на новый raw, чтобы не было скачка.
        double played = raw - _baseRawOffset;
        if (played < 0 || played > _playedSeconds + 30)
        {
            // окно сбросилось/склейка — фиксируем накопленное и берём новую базу
            _baseRawOffset = raw;
            played = _playedSeconds;
        }
        else
        {
            _playedSeconds = played;
        }

        long playing = _windowStartUnix + (long)_playedSeconds;
        AdvanceIfEnded(playing);
        RecalcBar(playing);
    }

    // пересчитать полосу из абсолютного unix текущего момента
    private void RecalcBar(long playingUnix)
    {
        if (IsLive) return;
        double total = Math.Max(1, _archiveProgramEnd - _archiveProgramStart);
        double pos = Math.Clamp(playingUnix - _archiveProgramStart, 0, total);
        DurationSeconds = total;
        PositionSeconds = pos;
        PositionText = Format(TimeSpan.FromSeconds(pos));
        DurationText = Format(TimeSpan.FromSeconds(total));

        // блок «сейчас в эфире» (архив) тикает вместе с позицией: прогресс и остаток
        if (_selectedProgram is not null)
        {
            ProgramProgress = pos / total;
            long leftMin = Math.Max(0, (long)(total - pos) / 60);
            TimeLeft = $"осталось {leftMin} мин";
        }
    }

    private static string Format(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                          : $"{t.Minutes}:{t.Seconds:D2}";

    // ===== EPG =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEpg))]
    private string _currentProgram = "";

    [ObservableProperty] private string _programTime = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgramColumns))]
    private double _programProgress;

    // доли для фирменного прогресса: заполнено* / осталось*
    public string ProgramColumns
    {
        get
        {
            double f = Math.Clamp(ProgramProgress, 0.001, 0.999);
            return $"{f.ToString(System.Globalization.CultureInfo.InvariantCulture)}*,{(1 - f).ToString(System.Globalization.CultureInfo.InvariantCulture)}*";
        }
    }
    [ObservableProperty] private string _timeLeft = "";
    [ObservableProperty] private string _nextProgram = "";
    [ObservableProperty] private string _nextTime = "";

    public bool HasEpg => !string.IsNullOrWhiteSpace(CurrentProgram);

    // ===== Избранное =====
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteColor))]
    private bool _isFavorite;

    public Color FavoriteColor => IsFavorite ? Color.FromArgb("#E5342B") : Color.FromArgb("#C7CEDA");

    [RelayCommand]
    private void ToggleFavorite() => IsFavorite = _tv.ToggleFavorite(ChannelId);

    // ===== Другие каналы =====
    public ObservableCollection<ChannelItem> OtherChannels { get; } = new();

    // ===== Передачи выбранного дня =====
    public ObservableCollection<EpgItem> DayEpg { get; } = new();

    // ===== Лента дней (сегодня + 14 назад) =====
    public ObservableCollection<DayTab> Days { get; } = new();

    private const int ArchiveDays = 14;
    private string _epgId = "";
    private string _channelIdForEpg = "";

    // кэш загруженных дней: ключ — DaysBack, значение — список передач
    private readonly Dictionary<int, List<EpgItem>> _dayCache = new();
    private long _serverNow;   // серверное время на момент открытия плеера

    private async Task LoadAsync()
    {
        try
        {
            var all = await _tv.GetChannelsAsync();

            var current = all.FirstOrDefault(c => c.Id == ChannelId);
            if (current is not null)
            {
                TileColor = current.TileColor;
                IconUrl = current.IconUrl;
                _rawLink = current.Link;
                StreamUrl = StreamUrlBuilder.Build(current.Link, 0); // 0 = прямой эфир
                IsLive = true;
                IsFavorite = _tv.IsFavorite(ChannelId);
                _epgId = current.EpgId;
                _channelIdForEpg = current.Id.ToString();
                if (string.IsNullOrWhiteSpace(ChannelName))
                    ChannelName = current.Name;
            }

            // другие каналы
            OtherChannels.Clear();
            foreach (var c in all.Where(c => c.Id != ChannelId).Take(10))
                OtherChannels.Add(c);

            // EPG
            await LoadEpgAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EPG] LoadAsync ИСКЛЮЧЕНИЕ: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task LoadEpgAsync()
    {
        var channelForEpg = _channelIdForEpg;
        if (string.IsNullOrWhiteSpace(channelForEpg)) return;

        try
        {
            // серверное время — один раз на сессию плеера
            _serverNow = await _tv.GetServerTimeAsync();

            // строим ленту дней: 0=Сегодня, 1=Вчера, далее даты
            BuildDayStrip();

            // открываем «Сегодня»
            await SelectDayAsync(Days.First());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EPG] ИСКЛЮЧЕНИЕ: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void BuildDayStrip()
    {
        Days.Clear();
        var today = DateTimeOffset.FromUnixTimeSeconds(_serverNow).LocalDateTime.Date;
        var culture = new System.Globalization.CultureInfo("ru-RU");

        for (int d = 0; d <= ArchiveDays; d++)
        {
            var date = today.AddDays(-d);
            string title = d switch
            {
                0 => "Сегодня",
                1 => "Вчера",
                _ => date.ToString("ddd d", culture),  // «сб 14»
            };
            Days.Add(new DayTab { Date = date, Title = title, DaysBack = d, IsSelected = d == 0 });
        }
    }

    [RelayCommand]
    private async Task SelectDayAsync(DayTab day)
    {
        if (day is null) return;

        foreach (var t in Days) t.IsSelected = t.DaysBack == day.DaysBack;

        // из кэша или с сервера
        if (!_dayCache.TryGetValue(day.DaysBack, out var items))
        {
            // unix начала выбранного дня (локальная полночь -> unix)
            long dayStart = new DateTimeOffset(day.Date, DateTimeOffset.Now.Offset).ToUnixTimeSeconds();
            var loaded = await _tv.GetEpgForDayAsync(_channelIdForEpg, dayStart);
            items = loaded.ToList();
            _dayCache[day.DaysBack] = items;
        }

        // флаги прошедшая/текущая/будущая относительно серверного времени
        DayEpg.Clear();
        EpgItem? current = null, next = null;
        for (int i = 0; i < items.Count; i++)
        {
            var e = items[i];
            long endUnix = i + 1 < items.Count ? items[i + 1].StartTimeUnix : long.MaxValue;
            e.IsCurrent = e.StartTimeUnix <= _serverNow && _serverNow < endUnix;
            e.IsPast = e.StartTimeUnix <= _serverNow;
            e.IsFuture = e.StartTimeUnix > _serverNow;
            if (e.IsCurrent) current = e;
            if (next is null && e.StartTimeUnix > _serverNow) next = e;
            DayEpg.Add(e);
        }

        // блок «сейчас в эфире»: для live — реальная текущая (сегодня);
        // для архива блок ведёт UpdateNowPlayingArchive (воспроизводимая передача)
        if (day.DaysBack == 0 && IsLive)
            UpdateNowPlaying(current, next);
    }

    private void UpdateNowPlaying(EpgItem? current, EpgItem? next)
    {
        if (current is not null)
        {
            CurrentProgram = current.Caption;
            if (next is not null)
            {
                ProgramTime = $"{current.StartTimeText} – {next.StartTimeText}";
                NextProgram = next.Caption;
                NextTime = next.StartTimeText;
                double total = next.StartTimeUnix - current.StartTimeUnix;
                double passed = _serverNow - current.StartTimeUnix;
                ProgramProgress = total > 0 ? Math.Clamp(passed / total, 0, 1) : 0;
                long leftMin = Math.Max(0, (next.StartTimeUnix - _serverNow) / 60);
                TimeLeft = $"осталось {leftMin} мин";
            }
            else
            {
                ProgramTime = current.StartTimeText;
                ProgramProgress = 0;
                TimeLeft = "";
                NextProgram = "";
            }
        }
        else
        {
            CurrentProgram = "Нет данных о текущей передаче";
            ProgramTime = "";
            ProgramProgress = 0;
            TimeLeft = "";
            NextProgram = "";
        }
    }

    [RelayCommand]
    private Task OpenChannelAsync(ChannelItem channel)
        => Shell.Current.GoToAsync($"player?id={channel.Id}&name={Uri.EscapeDataString(channel.Name)}");

    private string _rawLink = "";   // исходный Link с плейсхолдерами

    [RelayCommand]
    private void PlayArchive(EpgItem program)
    {
        if (program is null || !program.CanPlay) return;   // будущие не открываем

        SetSelected(program);
        IsLive = false;
        _archiveProgramStart = program.StartTimeUnix;

        // конец = начало следующей передачи (для длины полосы)
        var idx = DayEpg.IndexOf(program);
        _archiveProgramEnd = idx >= 0 && idx + 1 < DayEpg.Count
            ? DayEpg[idx + 1].StartTimeUnix
            : program.StartTimeUnix + 3600;

        UpdateNowPlayingArchive();   // блок «сейчас в эфире» = воспроизводимая передача
        OpenStreamAt(program.StartTimeUnix);
    }

    // блок «Сейчас в эфире» для АРХИВА: показывает воспроизводимую передачу,
    // прогресс считается от позиции воспроизведения (_windowStartUnix + _playedSeconds),
    // а НЕ от реального серверного времени.
    private void UpdateNowPlayingArchive()
    {
        if (_selectedProgram is null) return;

        CurrentProgram = _selectedProgram.Caption;
        ProgramTime = _selectedProgram.StartTimeText;

        var idx = DayEpg.IndexOf(_selectedProgram);
        if (idx >= 0 && idx + 1 < DayEpg.Count)
        {
            var next = DayEpg[idx + 1];
            ProgramTime = $"{_selectedProgram.StartTimeText} – {next.StartTimeText}";
            NextProgram = next.Caption;
            NextTime = next.StartTimeText;
        }
        else
        {
            NextProgram = "";
        }

        // прогресс и остаток — от текущей позиции воспроизведения в передаче
        long playingUnix = _windowStartUnix + (long)_playedSeconds;
        double total = Math.Max(1, _archiveProgramEnd - _archiveProgramStart);
        double passed = Math.Clamp(playingUnix - _archiveProgramStart, 0, total);
        ProgramProgress = total > 0 ? passed / total : 0;
        long leftMin = Math.Max(0, (long)(total - passed) / 60);
        TimeLeft = $"осталось {leftMin} мин";
    }

    [RelayCommand]
    private void PlayLive()
    {
        SetSelected(null);
        IsLive = true;
        CurrentProgram = "";
        _windowStartUnix = 0;
        StreamUrl = StreamUrlBuilder.Build(_rawLink, 0);
    }

    [RelayCommand]
    private Task BackAsync() => Shell.Current.GoToAsync("..");

    private static string MakeAbbrev(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "ТВ";
        var w = name.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (w.Length >= 2) return $"{char.ToUpper(w[0][0])}{char.ToUpper(w[1][0])}";
        return (w[0].Length >= 2 ? w[0][..2] : w[0]).ToUpperInvariant();
    }
}