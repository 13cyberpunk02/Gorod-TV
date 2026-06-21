using GorodTV.Core.Models;
using GorodTV.Core.Services;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class CategoriesTvPage : ContentPage
{
    private readonly CategoriesViewModel _vm;
    private bool _built;

    // размеры карточки под 960x540 (контент-зона ~876 шириной -> 3 в ряд)
    private const double CardWidth = 236;
    private const double CardHeight = 140;

    public CategoriesTvPage(CategoriesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
        BuildCards();
    }

    
    private void BuildCards()
    {
        CategoriesHost.Children.Clear();
        _built = false;

        foreach (var cat in _vm.Categories)
            CategoriesHost.Children.Add(BuildCard(cat));

        // стартовый фокус на первую карточку
        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(150);
            var first = CategoriesHost.GetVisualTreeDescendants().OfType<Button>().FirstOrDefault();
            first?.Focus();
            _built = true;
        });
    }

    // карточка = Grid (иконка+название+счётчик) + прозрачная фокус-кнопка поверх
    private View BuildCard(CategoryItem cat)
    {
        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#11161F"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            WidthRequest = CardWidth,
            HeightRequest = CardHeight,
            Margin = new Thickness(8)
        };

        var content = new Grid
        {
            Padding = new Thickness(20),
            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Auto },
            }
        };

        // иконка в цветном кружке
        var iconWrap = new Border
        {
            BackgroundColor = cat.Tint,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            WidthRequest = 48,
            HeightRequest = 48,
            HorizontalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = cat.FallbackGlyph,
                FontFamily = "MaterialSymbols",
                FontSize = 24,
                TextColor = cat.IconColor,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
        Grid.SetRow(iconWrap, 0);
        content.Children.Add(iconWrap);

        var title = new Label
        {
            Text = cat.Title,
            FontFamily = "OnestBold",
            FontSize = 18,
            TextColor = Colors.White,
            LineBreakMode = LineBreakMode.TailTruncation,
            VerticalOptions = LayoutOptions.End
        };
        Grid.SetRow(title, 1);
        content.Children.Add(title);

        var count = new Label
        {
            Text = cat.CountText,
            FontFamily = "Onest",
            FontSize = 13,
            TextColor = Color.FromArgb("#8A94A6")
        };
        Grid.SetRow(count, 2);
        content.Children.Add(count);

        // прозрачная фокус-кнопка поверх карточки (она focusable, ловит OK)
        var focus = new Button
        {
            BackgroundColor = Colors.Transparent,
            Style = (Style)Application.Current!.Resources["TvCardFocus"],
            CommandParameter = cat
        };
        focus.Clicked += OnCategoryClicked;
        // подсветка карточки при фокусе кнопки
        focus.Focused += (_, _) => { card.Stroke = Color.FromArgb("#1B66E5"); card.StrokeThickness = 3; };
        focus.Unfocused += (_, _) => { card.StrokeThickness = 0; };

        var overlay = new Grid();
        overlay.Children.Add(content);
        overlay.Children.Add(focus);   // поверх
        card.Content = overlay;

        return card;
    }

    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CategoryItem cat })
            await _vm.OpenCategoryCommand.ExecuteAsync(cat);
    }
}