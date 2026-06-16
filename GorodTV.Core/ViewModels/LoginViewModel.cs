using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorodTV.Core.Services;

namespace GorodTV.Core.ViewModels;

public partial class LoginViewModel(IGorodTvService tv, IDialogService dialogs) : ObservableObject
{
    private readonly IDialogService _dialogs = dialogs;
    private readonly IGorodTvService _tv = tv;

    [ObservableProperty]
    private string _contract = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EyeGlyph))]
    private bool _isPasswordHidden = true;

    public string EyeGlyph => IsPasswordHidden ? "\ue8f5" : "\ue8f4"; // visibility_off : visibility

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;
    public bool IsNotBusy => !IsBusy;

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Contract) || string.IsNullOrWhiteSpace(Password))
        {
            await _dialogs.AlertAsync("Не хватает данных",
                "Укажите номер договора и пароль, чтобы войти в кабинет.", AlertKind.Error);
            return;
        }

        try
        {
            IsBusy = true;
            var result = await _tv.LoginAsync(Contract.Trim(), Password);

            switch (result)
            {
                case AuthResult.Success:
                    await Shell.Current.GoToAsync("//categories");
                    break;
                case AuthResult.InvalidCredentials:
                    await _dialogs.AlertAsync("Неверные данные",
                        "Проверьте номер договора и пароль.", AlertKind.Error);
                    break;
                default:
                    await _dialogs.AlertAsync("Нет связи",
                        "Не удалось связаться с сервером. Проверьте работает ли интернет и попробуйте ещё раз.",
                        AlertKind.Error);
                    break;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }


    [RelayCommand]
    private Task ForgotPasswordAsync()
        => _dialogs.AlertAsync("Восстановление пароля",
            "Позвоните в абонентский отдел (33-111-00) или обратитесь в чате(Jivo) на сайте(info-lan.ru) — мы поможем восстановить доступ.");

    [RelayCommand]
    private Task ConnectAsync()
        => _dialogs.AlertAsync("Подключение",
            "Оставьте заявку на сайте Инфо-Лан(info-lan.ru) или позвоните нам по номеру 33-111-00 — подключим в ближайшее время.");
}
