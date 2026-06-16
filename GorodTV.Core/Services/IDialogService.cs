
namespace GorodTV.Core.Services;

public enum AlertKind { Info, Success, Error }

public interface IDialogService
{
    Task AlertAsync(string title, string message, AlertKind kind = AlertKind.Info);
    Task ToastAsync(string message);
    Task<bool> ConfirmAsync(string title, string message,
                        string accept = "Да", string cancel = "Отмена",
                        AlertKind kind = AlertKind.Info);
}
