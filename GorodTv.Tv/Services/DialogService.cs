
using CommunityToolkit.Maui.Extensions;
using GorodTv.Tv.Controls;
using GorodTV.Core.Services;

namespace GorodTv.Tv.Services;

public class DialogService : IDialogService
{
    private static Page CurrentPage =>
      Shell.Current?.CurrentPage
      ?? Application.Current!.Windows[0].Page!;

    public async Task AlertAsync(string title, string message, AlertKind kind = AlertKind.Info)
    {
        var popup = new TvDialogPopup(title, message, kind, "ОК");
        await CurrentPage.ShowPopupAsync<bool>(popup);
    }

    public async Task<bool> ConfirmAsync(string title, string message,
        string accept = "Да", string cancel = "Отмена", AlertKind kind = AlertKind.Info)
    {
        var popup = new TvDialogPopup(title, message, accept, cancel, kind, confirm: true);
        var result = await CurrentPage.ShowPopupAsync<bool>(popup);
        if (result.WasDismissedByTappingOutsideOfPopup) return false;
        return result.Result;
    }

    // Toast на ТВ: лёгкое уведомление снизу по центру, само исчезает.
    public async Task ToastAsync(string message)
    {
        var page = CurrentPage;
        if (page is null) return;

        var toast = new Border
        {
            BackgroundColor = Color.FromArgb("#11161F"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Padding = new Thickness(28, 16),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 0, 60),
            Opacity = 0,
            Content = new Label
            {
                Text = message,
                FontFamily = "OnestMedium",
                FontSize = 18,
                TextColor = Colors.White
            }
        };

        // показываем поверх контента страницы, если это возможно
        if (page is ContentPage cp && cp.Content is Layout root)
        {
            // оборачиваем в Grid, чтобы наложить тост поверх
            if (root is Grid grid)
            {
                grid.Children.Add(toast);
                Grid.SetRowSpan(toast, Math.Max(1, grid.RowDefinitions.Count));
                Grid.SetColumnSpan(toast, Math.Max(1, grid.ColumnDefinitions.Count));
            }
            else
            {
                return; // не мешаем, если корень не Grid
            }

            await toast.FadeTo(1, 180);
            await Task.Delay(2200);
            await toast.FadeTo(0, 250);
            grid?.Children.Remove(toast);
        }
    }
}
