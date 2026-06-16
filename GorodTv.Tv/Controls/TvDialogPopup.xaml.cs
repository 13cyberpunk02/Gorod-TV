using CommunityToolkit.Maui.Views;
using GorodTV.Core.Services;

namespace GorodTv.Tv.Controls;

public partial class TvDialogPopup : Popup<bool>
{
    // алерт: одна кнопка
    public TvDialogPopup(string title, string message, AlertKind kind, string okText)
    {
        InitializeComponent();
        Setup(title, message, kind);
        OkButton.Text = okText;
        CancelButton.IsVisible = false;
        Opened += OnPopupOpened;
    }

    // подтверждение: две кнопки
    public TvDialogPopup(string title, string message, string accept, string cancel, AlertKind kind, bool confirm)
    {
        InitializeComponent();
        Setup(title, message, kind);
        OkButton.Text = accept;
        CancelButton.Text = cancel;
        CancelButton.IsVisible = true;
        Opened += OnPopupOpened;
    }

    // стартовый фокус на главной кнопке — критично для пульта
    private void OnPopupOpened(object? sender, EventArgs e)
    {
        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(150);
            OkButton.Focus();
        });
    }

    private void Setup(string title, string message, AlertKind kind)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;

        var (glyph, fg, bg) = kind switch
        {
            AlertKind.Success => ("\uf0be", "#2ED573", "#16301F"),
            AlertKind.Error => ("\uf8b6", "#E5342B", "#3A1714"),
            _ => ("\ue88e", "#1B66E5", "#13233D"),
        };
        IconLabel.Text = glyph;
        IconLabel.TextColor = Color.FromArgb(fg);
        IconCircle.BackgroundColor = Color.FromArgb(bg);
    }

    private async void OnOk(object? sender, EventArgs e) => await CloseAsync(true);
    private async void OnCancel(object? sender, EventArgs e) => await CloseAsync(false);
}