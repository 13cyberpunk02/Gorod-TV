using CommunityToolkit.Maui.Core;
using GorodTV.Core.Services;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class PlayerTvPage : ContentPage
{
    private readonly PlayerViewModel _vm;
    private readonly IGorodTvService _tv;
    private IDispatcherTimer? _hideTimer;

    private bool _seekFocused;       // фокус на seek-баре
    private bool _seeking;           // идёт мотание (предпросмотр не зафиксирован)
    private double _seekPreview;     // предпросматриваемая позиция (сек) при мотании

    public PlayerTvPage(PlayerViewModel vm, IGorodTvService tv)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _tv = tv;

        Media.PositionChanged += OnPositionChanged;
        Media.StateChanged += OnStateChanged;

#if ANDROID
        // убрать нативный квадратный фон/подсветку у прозрачной seek-кнопки
        SeekFocus.HandlerChanged += (_, _) =>
        {
            // MAUI рендерит Button как MaterialButton, который сам рисует фон и
            // подсветку фокуса. MaterialButton наследует Android.Widget.Button,
            // поэтому базовое чистим напрямую, а специфичные свойства (tint/ripple)
            // — через рефлексию, чтобы не зависеть от namespace биндинга.
            if (SeekFocus.Handler?.PlatformView is Android.Views.View v)
            {
                var transparent = Android.Content.Res.ColorStateList.ValueOf(
                    Android.Graphics.Color.Transparent);

                v.Background = null;
                v.Foreground = null;          // квадрат подсветки фокуса
                v.StateListAnimator = null;   // тень/elevation
                v.Elevation = 0;
                v.SetPadding(0, 0, 0, 0);

                // BackgroundTintList / RippleColor / StrokeWidth — если это MaterialButton
                var t = v.GetType();
                try { t.GetProperty("BackgroundTintList")?.SetValue(v, transparent); } catch { }
                try { t.GetProperty("RippleColor")?.SetValue(v, transparent); } catch { }
                try { t.GetProperty("StrokeWidth")?.SetValue(v, 0); } catch { }
            }
        };
#endif

        // прогресс архива -> ширина заполнения
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlayerViewModel.ProgramProgress))
                Dispatcher.Dispatch(UpdateProgressFill);
            else if (e.PropertyName == nameof(PlayerViewModel.StreamUrl))
                Dispatcher.Dispatch(ReopenStream);
        };

        ProgressTrack.SizeChanged += (_, _) => UpdateProgressFill();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.IsFavorite = _tv.IsFavorite(_vm.ChannelId);   // актуальное сердце
        ReopenStream();
#if ANDROID
        MainActivity.RemoteKey += OnRemoteKey;
#endif
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
#if ANDROID
        MainActivity.RemoteKey -= OnRemoteKey;
#endif
        _hideTimer?.Stop();
        Media.Stop();
        Media.Handler?.DisconnectHandler();
#if ANDROID
        MainActivity.SwallowHorizontalKeys = false;
#endif
    }

    // ===== поток =====
    private void ReopenStream()
    {
        if (string.IsNullOrEmpty(_vm.StreamUrl)) return;
        Media.Source = _vm.StreamUrl;
        Media.Play();
    }

    private void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
        => _vm.UpdateProgress(e.Position, Media.Duration);

    private void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
        => _vm.IsPlaying = e.NewState == MediaElementState.Playing;

    // ===== контролы =====
    private void ShowControls()
    {
        ControlsOverlay.IsVisible = true;
        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(100);
            PlayPauseBtn.Focus();   // фокус на паузу
        });
        RestartHideTimer();
    }

    private void RestartHideTimer()
    {
        _hideTimer?.Stop();
        _hideTimer = Dispatcher.CreateTimer();
        _hideTimer.Interval = TimeSpan.FromSeconds(4);
        _hideTimer.IsRepeating = false;
        _hideTimer.Tick += (_, _) => ControlsOverlay.IsVisible = false;
        _hideTimer.Start();
    }

    private void OnSeekFocused(object? sender, FocusEventArgs e)
    {
        _seekFocused = true;
        _seekPreview = _vm.PositionSeconds;
        SeekThumb.Scale = 1.5;          // маркер крупнее = бар активен
        SeekThumb.BackgroundColor = Colors.White;
#if ANDROID
        MainActivity.SwallowHorizontalKeys = true;   // ←→ не уводят фокус
#endif
        RestartHideTimer();
    }

    private void OnSeekUnfocused(object? sender, FocusEventArgs e)
    {
        _seekFocused = false;
        _seeking = false;
        SeekThumb.Scale = 1.0;
#if ANDROID
        MainActivity.SwallowHorizontalKeys = false;
#endif
    }

    private void OnToggleFavorite(object? sender, EventArgs e)
    {
        // VM.ToggleFavorite теперь сам пишет в хранилище (через сервис)
        _vm.ToggleFavoriteCommand.Execute(null);
        RestartHideTimer();
    }

    private void OnPlayPause(object? sender, EventArgs e)
    {
        if (Media.CurrentState == MediaElementState.Playing) Media.Pause();
        else Media.Play();
        RestartHideTimer();
    }

    // ===== EPG-панель =====
    private void OnOpenEpg(object? sender, EventArgs e)
    {
        _hideTimer?.Stop();
        ControlsOverlay.IsVisible = false;

        // дни/передачи загружаются автоматически при открытии канала (LoadEpgAsync).
        // здесь просто строим UI из готовых _vm.Days / _vm.DayEpg.
        BuildDays();
        BuildPrograms();
        EpgPanel.IsVisible = true;

        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(120);
            var first = DaysHost.Children.OfType<Button>().FirstOrDefault();
            first?.Focus();
        });
    }

    private void BuildDays()
    {
        DaysHost.Children.Clear();
        foreach (var day in _vm.Days)
        {
            var captured = day;
            var btn = new Button
            {
                Text = day.Title,
                Style = (Style)Application.Current!.Resources["TvDayChip"],
                CommandParameter = day
            };
            btn.Clicked += async (_, _) =>
            {
                await _vm.SelectDayCommand.ExecuteAsync(captured);
                BuildPrograms();
            };
            DaysHost.Children.Add(btn);
        }
    }

    private void BuildPrograms()
    {
        ProgramsHost.Children.Clear();
        foreach (var prog in _vm.DayEpg)
        {
            var captured = prog;
            var btn = new Button
            {
                Text = $"{prog.StartTimeText}   {prog.Caption}",
                Style = (Style)Application.Current!.Resources["TvEpgRow"],
                CommandParameter = prog,
                IsEnabled = prog.CanPlay   // будущие передачи не выбрать
            };
            btn.Clicked += (_, _) =>
            {
                if (!captured.CanPlay) return;
                _vm.PlayArchiveCommand.Execute(captured);
                EpgPanel.IsVisible = false;
            };
            ProgramsHost.Children.Add(btn);
        }
    }

    // ===== прогресс архива =====
    private void UpdateProgressFill()
    {
        if (_seeking) return;   // во время мотания не трогаем — рисует PreviewSeek
        double w = ProgressTrack.Width;
        if (w <= 0) return;
        double frac = Math.Clamp(_vm.ProgramProgress, 0, 1);
        ProgressFill.WidthRequest = frac * w;
        SeekThumb.TranslationX = frac * w - SeekThumb.WidthRequest / 2;
    }

    // ===== D-pad =====
    // аппаратные клавиши пульта: OK показать/пауза, ←→ перемотка (архив), Back выход
    protected override bool OnBackButtonPressed()
    {
        if (EpgPanel.IsVisible)
        {
            EpgPanel.IsVisible = false;
            return true;   // Back закрывает EPG-панель
        }
        if (ControlsOverlay.IsVisible)
        {
            ControlsOverlay.IsVisible = false;
            return true;   // затем прячет контролы
        }
        return base.OnBackButtonPressed();   // затем выход со страницы
    }

    // вызывается из MainActivity при нажатии клавиш пульта (см. README — подписка)
    public void OnRemoteKey(string key)
    {
        // когда открыта EPG-панель — пультом управляет она
        if (EpgPanel.IsVisible) return;

        switch (key)
        {
            case "DpadCenter":
            case "Enter":
                // контролы скрыты -> показать (на экране ещё нет кнопок, дублей нет)
                if (!ControlsOverlay.IsVisible) { ShowControls(); break; }
                // на seek-баре нет Clicked -> применяем перемотку здесь
                if (_seekFocused) ApplySeek();
                // иначе фокус на обычной кнопке -> её Clicked сработает сам,
                // тут НИЧЕГО не делаем (иначе двойное срабатывание)
                break;

            case "DpadDown":
            case "DpadUp":
                if (!ControlsOverlay.IsVisible) ShowControls();
                // вверх/вниз — обычная навигация фокусом (вниз к seek, вверх к кнопкам)
                break;

            case "DpadLeft":
                if (_seekFocused) { PreviewSeek(-30); }   // мотаем ТОЛЬКО на seek-баре
                break;

            case "DpadRight":
                if (_seekFocused) { PreviewSeek(30); }
                break;
        }
    }

    // предпросмотр позиции при мотании (двигаем бегунок, поток пока не трогаем)
    private void PreviewSeek(double deltaSeconds)
    {
        if (!_vm.IsArchive) return;
        _seeking = true;   // блокируем перерисовку по реальной позиции
        _seekPreview = Math.Clamp(_seekPreview + deltaSeconds, 0, _vm.DurationSeconds);
        double frac = _vm.DurationSeconds > 0 ? _seekPreview / _vm.DurationSeconds : 0;
        double w = ProgressTrack.Width;
        ProgressFill.WidthRequest = frac * w;
        SeekThumb.TranslationX = frac * w - SeekThumb.WidthRequest / 2;
        RestartHideTimer();
    }

    // применить перемотку (OK) — переоткрыть поток с выбранной точки
    private void ApplySeek()
    {
        if (!_vm.IsArchive) return;
        _vm.SeekToFraction(_seekPreview);
        _seeking = false;   // перемотка применена -> снова слушаем реальную позицию
        RestartHideTimer();
    }
}