using CommunityToolkit.Maui.Core;
using GorodTV.Core.ViewModels;

namespace GorodTV.Pages;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _vm;
    private IDispatcherTimer? _hideTimer;

    public PlayerPage(PlayerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

        Media.PositionChanged += OnPositionChanged;
        Media.StateChanged += OnMediaStateChanged;
        ProgramTrack.SizeChanged += (_, _) => UpdateProgramFill();

        _vm.FullscreenToggleRequested += ApplyFullscreen;
        
        _vm.PropertyChanged += OnVmPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RestartHideTimer();
        DeviceDisplay.Current.MainDisplayInfoChanged += OnDisplayChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.Current.MainDisplayInfoChanged -= OnDisplayChanged;
        _vm.PropertyChanged -= OnVmPropertyChanged;

        SetOrientation(portrait: true);
        ShowSystemBars(true);

        _hideTimer?.Stop();
        Media.Stop();
        Media.Handler?.DisconnectHandler();
    }

    private void OnDisplayChanged(object? sender, DisplayInfoChangedEventArgs e)
    {
        bool isLandscape = e.DisplayInfo.Orientation == DisplayOrientation.Landscape;

        if (string.IsNullOrEmpty(_vm.StreamUrl)) return;
        _vm.OnOrientationChanged(isLandscape);
    }

    private void ApplyFullscreen(bool on)
    {
        if (on)
        {
            VideoArea.HeightRequest = -1;          
            Grid.SetRowSpan(VideoArea, 2);         
            InfoArea.IsVisible = false;
            Padding = 0;                           
            Shell.SetTabBarIsVisible(this, false); 
            Shell.SetNavBarIsVisible(this, false);
            ShowSystemBars(false);
            SetOrientation(portrait: false);       
        }
        else
        {
            VideoArea.HeightRequest = 230;
            Grid.SetRowSpan(VideoArea, 1);
            InfoArea.IsVisible = true;
            Padding = 0;
            Shell.SetTabBarIsVisible(this, true);
            Shell.SetNavBarIsVisible(this, false); 
            ShowSystemBars(true);
            SetOrientation(portrait: true);
        }
        RestartHideTimer();
    }

    private void OnVideoTapped(object? sender, TappedEventArgs e)
    {
        _vm.ControlsVisible = !_vm.ControlsVisible;
        if (_vm.ControlsVisible) RestartHideTimer();
    }

    private void RestartHideTimer()
    {
        _hideTimer?.Stop();
        _hideTimer = Dispatcher.CreateTimer();
        _hideTimer.Interval = TimeSpan.FromSeconds(4);
        _hideTimer.IsRepeating = false;
        _hideTimer.Tick += (_, _) => _vm.ControlsVisible = false;
        _hideTimer.Start();
    }

    private void OnPlayPauseTapped(object? sender, TappedEventArgs e)
    {
        if (Media.CurrentState == MediaElementState.Playing)
            Media.Pause();
        else
            Media.Play();
        RestartHideTimer();
    }

    private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
        => _vm.IsPlaying = e.NewState == MediaElementState.Playing;

    protected override bool OnBackButtonPressed()
    {
        if (_vm.IsFullscreen)
        {
            _vm.IsFullscreen = false;
            ApplyFullscreen(false);
            return true;
        }
        return base.OnBackButtonPressed();
    }

    private void OnSeekDragCompleted(object? sender, EventArgs e)
    {
        if (sender is Slider s)
            _vm.SeekToFraction(s.Value * _vm.DurationSeconds);
        _userDragging = false;
        RestartHideTimer();
    }

    private bool _userDragging;

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerViewModel.Progress01) && !_userDragging)
        {
            Dispatcher.Dispatch(() => SeekSlider.Value = _vm.Progress01);
        }
        else if (e.PropertyName == nameof(PlayerViewModel.ProgramProgress))
        {
            Dispatcher.Dispatch(UpdateProgramFill);
        }
    }

    private void UpdateProgramFill()
    {
        double w = ProgramTrack.Width;
        if (w <= 0) return;
        ProgramFill.WidthRequest = Math.Clamp(_vm.ProgramProgress, 0, 1) * w;
    }

    private void OnSeekDragStarted(object? sender, EventArgs e)
    {
        _userDragging = true;
        _hideTimer?.Stop();
    }

    private void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
        => _vm.UpdateProgress(e.Position, Media.Duration);

    private static void SetOrientation(bool portrait)
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        if (activity is null) return;
        activity.RequestedOrientation = portrait
            ? Android.Content.PM.ScreenOrientation.Portrait
            : Android.Content.PM.ScreenOrientation.Landscape;
#endif
    }

    private void ShowSystemBars(bool show)
    {
#if ANDROID
        var activity = Platform.CurrentActivity;
        var window = activity?.Window;
        if (window is null) return;

        if (OperatingSystem.IsAndroidVersionAtLeast(28) && window.Attributes is not null)
        {
            window.Attributes.LayoutInDisplayCutoutMode = show
                ? Android.Views.LayoutInDisplayCutoutMode.Default
                : Android.Views.LayoutInDisplayCutoutMode.ShortEdges;
        }

        var controller = new AndroidX.Core.View.WindowInsetsControllerCompat(window, window.DecorView);
        var statusBars = AndroidX.Core.View.WindowInsetsCompat.Type.StatusBars();
        var navBars = AndroidX.Core.View.WindowInsetsCompat.Type.NavigationBars();

        if (show)
        {
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, true);
            controller.Show(statusBars);
            controller.Show(navBars);
            window.ClearFlags(Android.Views.WindowManagerFlags.Fullscreen);
        }
        else
        {
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, false);
            controller.Hide(statusBars);
            controller.Hide(navBars);
            controller.SystemBarsBehavior =
                AndroidX.Core.View.WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
        }
#endif
    }
}