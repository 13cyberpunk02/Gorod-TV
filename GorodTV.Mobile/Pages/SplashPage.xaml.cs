using GorodTV.Core.Services;

namespace GorodTV.Pages;

public partial class SplashPage : ContentPage
{
    private readonly IGorodTvService _tv;
    public SplashPage(IGorodTvService tv)
	{
		InitializeComponent();
        _tv = tv;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // лёгкое движение фоновых эллипсов параллельно с логотипом
        _ = SplashBlob1.TranslateToAsync(70, -80, 2600, Easing.SinInOut);
        _ = SplashBlob2.TranslateToAsync(-50, 50, 2600, Easing.SinInOut);

        // 1. появление: масштаб + прозрачность
        await Task.WhenAll(
            Logo.FadeToAsync(1, 350, Easing.CubicOut),
            Logo.ScaleToAsync(1, 350, Easing.CubicOut));

        // 2. поворот на 360°
        await Logo.RotateToAsync(360, 900, Easing.CubicInOut);
        Logo.Rotation = 0;

        // 3. прыжок
        await Logo.TranslateToAsync(0, -60, 260, Easing.CubicOut);
        await Logo.TranslateToAsync(0, 0, 600, Easing.BounceOut);

        await Task.Delay(300);

        // 4. РЕШЕНИЕ о маршруте принимается ТОЛЬКО здесь (одно место — нет гонки)
        bool restored;
        try
        {
            restored = await _tv.TryRestoreSessionAsync();
        }
        catch
        {
            restored = false;
        }

        System.Diagnostics.Debug.WriteLine($"[AUTH] Splash -> restored={restored}");
        await Shell.Current.GoToAsync(restored ? "//categories" : "//login");
    }
}