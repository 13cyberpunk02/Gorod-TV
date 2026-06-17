
using GorodTV.Core.Models;

namespace GorodTv.Tv.Controls;

// общий построитель карточки канала (используют список каналов и избранное)
public static class TvChannelCard
{
    public static View Build(ChannelItem ch, double cardWidth, double previewHeight,
                             EventHandler clickHandler)
    {
        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#11161F"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            WidthRequest = cardWidth,
            HeightRequest = previewHeight + 70,   // превью + блок названия/передачи
            Margin = new Thickness(8),
            Padding = 0
        };

        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition { Height = previewHeight },
                new RowDefinition { Height = GridLength.Auto },
            },
            RowSpacing = 0
        };

        // превью: логотип канала крупно (если есть), иначе цветной фон с аббревиатурой
        View previewContent;
        if (ch.HasIcon)
        {
            previewContent = new Image
            {
                Source = ch.IconUrl,
                Aspect = Aspect.AspectFit,
                Margin = new Thickness(24),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
        }
        else
        {
            previewContent = new Label
            {
                Text = ch.Abbrev,
                FontFamily = "OnestBold",
                FontSize = 32,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
        }

        var preview = new Grid
        {
            BackgroundColor = ch.HasIcon ? Color.FromArgb("#1E2530") : ch.TileColor,
            Padding = new Thickness(8)
        };
        preview.Children.Add(previewContent);

        // бейдж ЭФИР поверх превью (если идёт эфир)
        if (ch.IsLive)
        {
            preview.Children.Add(new Border
            {
                BackgroundColor = Color.FromArgb("#E5342B"),
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(8, 3),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Content = new Label
                {
                    Text = "● ЭФИР",
                    FontFamily = "OnestBold",
                    FontSize = 10,
                    TextColor = Colors.White
                }
            });
        }
        Grid.SetRow(preview, 0);
        root.Children.Add(preview);

        var info = new VerticalStackLayout { Padding = new Thickness(14, 10, 14, 12), Spacing = 2 };
        info.Children.Add(new Label
        {
            Text = ch.Id > 0 ? $"{ch.Name} · {ch.Id}" : ch.Name,
            FontFamily = "OnestBold",
            FontSize = 15,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation
        });

        // строка текущей передачи (EPG подгружается асинхронно -> обновим по событию)
        var programLabel = new Label
        {
            Text = string.IsNullOrWhiteSpace(ch.CurrentProgram) ? " " : ch.CurrentProgram,
            FontFamily = "Onest",
            FontSize = 12,
            TextColor = Color.FromArgb("#8A94A6"),
            LineBreakMode = LineBreakMode.TailTruncation
        };
        info.Children.Add(programLabel);

        ch.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChannelItem.CurrentProgram))
            {
                var text = ch.CurrentProgram;
                programLabel.Dispatcher.Dispatch(() =>
                    programLabel.Text = string.IsNullOrWhiteSpace(text) ? " " : text);
            }
        };
        Grid.SetRow(info, 1);
        root.Children.Add(info);

        var focus = new Button
        {
            BackgroundColor = Colors.Transparent,
            Style = (Style)Application.Current!.Resources["TvCardFocus"],
            CommandParameter = ch
        };
        focus.Clicked += clickHandler;
        focus.Focused += (_, _) => { card.Stroke = Color.FromArgb("#1B66E5"); card.StrokeThickness = 3; };
        focus.Unfocused += (_, _) => { card.StrokeThickness = 0; };

        var overlay = new Grid();
        overlay.Children.Add(root);
        overlay.Children.Add(focus);
        card.Content = overlay;
        return card;
    }
}