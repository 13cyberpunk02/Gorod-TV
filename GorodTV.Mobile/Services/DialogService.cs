
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using GorodTV.Controls;
using GorodTV.Core.Services;

namespace GorodTV.Services;


public class DialogService : IDialogService
{
    public async Task AlertAsync(string title, string message, AlertKind kind = AlertKind.Info)
    {
        var popup = new BrandAlertPopup(title, message, kind);
        var page = Shell.Current?.CurrentPage ?? Application.Current!.Windows[0].Page!;
        await page.ShowPopupAsync<bool>(popup);
    }

    public Task ToastAsync(string message)
        => MainThread.InvokeOnMainThreadAsync(() =>
            Toast.Make(message, ToastDuration.Short, 14).Show());

    public async Task<bool> ConfirmAsync(string title, string message,
        string accept = "Да", string cancel = "Отмена", AlertKind kind = AlertKind.Info)
    {
        var popup = new BrandAlertPopup(title, message, accept, cancel, kind, confirm: true);
        var page = Shell.Current?.CurrentPage ?? Application.Current!.Windows[0].Page!;

        var result = await page.ShowPopupAsync<bool>(popup);

        if (result.WasDismissedByTappingOutsideOfPopup)
            return false;

        return result.Result;
    }
}
