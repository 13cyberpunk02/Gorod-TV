using GorodTV.Core.Services;

namespace GorodTv.Tv.Pages;

public partial class SplashTvPage : ContentPage
{
    private readonly IGorodTvService _tv;
    public SplashTvPage(IGorodTvService tv)
	{
		InitializeComponent();
		_tv = tv;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(400);   // короткая заставка

        bool restored;
        try { restored = await _tv.TryRestoreSessionAsync(); }
        catch { restored = false; }

        // авторизованы -> категории, иначе -> логин
        await Shell.Current.GoToAsync(restored ? "//categories" : "//login");
    }
}