using GorodTV.Core.Services;

namespace GorodTv.Tv.Controls;

public partial class TvSidebar : ContentView
{
	public TvSidebar()
	{
		InitializeComponent();
	}

    // диалог-сервис берём из DI через текущее приложение (для подтверждения выхода)
    private static IDialogService? Dialogs =>
        (Application.Current?.Handler?.MauiContext?.Services)?.GetService(typeof(IDialogService)) as IDialogService;

    private static IGorodTvService? Tv =>
        (Application.Current?.Handler?.MauiContext?.Services)?.GetService(typeof(IGorodTvService)) as IGorodTvService;

    private async void OnCategories(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//categories");

    private async void OnFavorites(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//favorites");

    private async void OnLogout(object? sender, EventArgs e)
    {
        var dialogs = Dialogs;
        bool ok = dialogs is null
            ? true
            : await dialogs.ConfirmAsync("Выйти из аккаунта?",
                "Нужно будет снова ввести номер договора и пароль.",
                "Выйти", "Отмена", AlertKind.Info);
        if (!ok) return;
        Tv?.Logout();
        await Shell.Current.GoToAsync("//login");
    }
}