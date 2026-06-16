using CommunityToolkit.Maui.Views;
using GorodTV.Core.Services;


namespace GorodTV.Controls;

// Popup<bool>: позволяет CloseAsync(true/false) и чтение результата через IPopupResult<bool>
public partial class BrandAlertPopup : Popup<bool>
{
    // Обычный алерт (одна кнопка). Результат не важен.
    public BrandAlertPopup(string title, string message,
                           AlertKind kind = AlertKind.Info,
                           string buttonText = "Понятно")
    {
        InitializeComponent();
        Setup(title, message, kind);
        OkButton.Text = buttonText;
        CancelButton.IsVisible = false;
    }

    // Диалог подтверждения (две кнопки).
    public BrandAlertPopup(string title, string message,
                           string accept, string cancel,
                           AlertKind kind, bool confirm)
    {
        InitializeComponent();
        Setup(title, message, kind);
        OkButton.Text = accept;
        CancelButton.Text = cancel;
        CancelButton.IsVisible = true;
    }

    private void Setup(string title, string message, AlertKind kind)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;

        var (glyph, fg, bg) = kind switch
        {
            AlertKind.Success => ("\uf0be", "#1FA764", "#E3F6EC"), // check_circle
            AlertKind.Error => ("\uf8b6", "#E5342B", "#FCE7E6"),   // error
            _ => ("\ue88e", "#1B66E5", "#E4EDFC"),                 // info
        };

        IconLabel.Text = glyph;
        IconLabel.TextColor = Color.FromArgb(fg);
        IconCircle.BackgroundColor = Color.FromArgb(bg);
    }

    // основная кнопка -> true, отмена -> false
    private async void OnOkClicked(object? sender, EventArgs e)
        => await CloseAsync(true);

    private async void OnCancelClicked(object? sender, EventArgs e)
        => await CloseAsync(false);
}
