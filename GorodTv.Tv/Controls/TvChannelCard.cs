
using GorodTV.Core.Models;

namespace GorodTv.Tv.Controls;

// Лёгкий построитель карточки канала (список + избранное).
// Оптимизировано для слабых ТВ-боксов: минимум вложенных элементов.
public static class TvChannelCard
{
    public static View Build(ChannelItem ch, double cardWidth, double previewHeight,
                             EventHandler clickHandler)
    {
        // корневой Grid карточки: превью (фикс. высота) + подпись
        var root = new Grid
        {
            WidthRequest = cardWidth,
            HeightRequest = previewHeight + 64,
            Margin = new Thickness(8),
            BackgroundColor = Color.FromArgb("#11161F"),
            RowDefinitions =
            {
                new RowDefinition { Height = previewHeight },
                new RowDefinition { Height = GridLength.Auto },
            }
        };

        // --- превью ---
        if (ch.HasIcon)
        {
            root.Add(new Image
            {
                Source = ch.IconUrl,
                Aspect = Aspect.AspectFit,
                Margin = new Thickness(18),
                BackgroundColor = Color.FromArgb("#1E2530")
            }, 0, 0);
        }
        else
        {
            root.Add(new Label
            {
                Text = ch.Abbrev,
                FontFamily = "OnestBold",
                FontSize = 26,
                TextColor = Colors.White,
                BackgroundColor = ch.TileColor,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }, 0, 0);
        }

        // бейдж ЭФИР (лёгкий Label, без отдельного Border-контейнера)
        if (ch.IsLive)
        {
            root.Add(new Label
            {
                Text = "● ЭФИР",
                FontFamily = "OnestBold",
                FontSize = 10,
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#E5342B"),
                Padding = new Thickness(7, 3),
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(10)
            }, 0, 0);
        }

        // --- подпись: название + передача ---
        var info = new VerticalStackLayout { Padding = new Thickness(12, 8, 12, 10), Spacing = 1 };
        info.Add(new Label
        {
            Text = ch.Id > 0 ? $"{ch.Name} · {ch.Id}" : ch.Name,
            FontFamily = "OnestBold",
            FontSize = 14,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation
        });

        var programLabel = new Label
        {
            Text = string.IsNullOrWhiteSpace(ch.CurrentProgram) ? " " : ch.CurrentProgram,
            FontFamily = "Onest",
            FontSize = 11,
            TextColor = Color.FromArgb("#8A94A6"),
            LineBreakMode = LineBreakMode.TailTruncation
        };
        info.Add(programLabel);
        root.Add(info, 0, 1);

        // обновление строки передачи когда EPG догрузится (лениво/батчами)
        ch.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChannelItem.CurrentProgram))
            {
                var t = ch.CurrentProgram;
                programLabel.Dispatcher.Dispatch(() =>
                    programLabel.Text = string.IsNullOrWhiteSpace(t) ? " " : t);
            }
        };

        // фокус-кнопка поверх (прозрачная) — подсветка рамкой
        var focus = new Button
        {
            BackgroundColor = Colors.Transparent,
            BorderColor = Colors.Transparent,
            BorderWidth = 0,
            CommandParameter = ch
        };
        focus.Clicked += clickHandler;
        focus.Focused += (_, _) => { root.BackgroundColor = Color.FromArgb("#1B2740"); };
        focus.Unfocused += (_, _) => { root.BackgroundColor = Color.FromArgb("#11161F"); };
        root.Add(focus, 0, 0);
        Grid.SetRowSpan(focus, 2);

        return root;
    }
}
