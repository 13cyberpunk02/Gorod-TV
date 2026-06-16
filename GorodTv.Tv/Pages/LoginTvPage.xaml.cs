using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class LoginTvPage : ContentPage
{
    private readonly LoginViewModel _vm;

    private bool _editingContract = true;   // активное поле: договор/пароль
    private bool _lettersLayout;            // false — цифры, true — латиница
    private Button? _nextFieldButton;       // кнопка смены активного поля
    public LoginTvPage(LoginViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        _vm = vm;

        BuildKeyboard();

        Loaded += (_, _) =>
        {
            var first = KeyboardHost.GetVisualTreeDescendants().OfType<Button>().FirstOrDefault();
            first?.Focus();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshFieldTexts();
        UpdateFieldHighlight();
        UpdateNextFieldLabel();
    }

    // ===== Построение клавиатуры =====
    private void BuildKeyboard()
    {
        KeyboardHost.Children.Clear();

        try
        {
            if (_lettersLayout)
                BuildLetters();   // латиница, 7 колонок
            else
                BuildDigits();    // цифры, 5 колонок
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[KBD] ошибка построения: {ex}");
            KeyboardHost.Children.Add(new Label
            {
                Text = "KBD ERROR: " + ex.Message,
                TextColor = Colors.Red,
                FontSize = 11
            });
        }

        // сервисный ряд (общий для обеих раскладок)
        KeyboardHost.Children.Add(BuildServiceRow());
        // «Войти» — статичная кнопка в XAML (Grid.Row=4), здесь не дублируем
    }

    private Style? GetStyle(string key)
    {
        // ищем стиль в ресурсах приложения и страницы (merged dictionaries тоже)
        if (Application.Current!.Resources.TryGetValue(key, out var appStyle))
            return appStyle as Style;
        if (Resources.TryGetValue(key, out var pageStyle))
            return pageStyle as Style;
        System.Diagnostics.Debug.WriteLine($"[KBD] стиль '{key}' не найден");
        return null;
    }

    private void BuildDigits()
    {
        string[] keys = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
        KeyboardHost.Children.Add(BuildKeyGrid(keys, columns: 5));
    }

    private void BuildLetters()
    {
        string[] keys =
        {
            "a","b","c","d","e","f","g",
            "h","i","j","k","l","m","n",
            "o","p","q","r","s","t","u",
            "v","w","x","y","z"
        };
        KeyboardHost.Children.Add(BuildKeyGrid(keys, columns: 7));
    }

    // строит сетку символьных клавиш
    private Grid BuildKeyGrid(string[] keys, int columns)
    {
        var grid = new Grid { ColumnSpacing = 2, RowSpacing = 2 };
        int rows = (int)Math.Ceiling(keys.Length / (double)columns);
        for (int c = 0; c < columns; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        for (int r = 0; r < rows; r++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (int i = 0; i < keys.Length; i++)
        {
            var btn = new Button
            {
                Text = keys[i],
                Style = GetStyle("TvKey"),
                HeightRequest = 44
            };
            btn.Clicked += OnKey;
            int row = i / columns, col = i % columns;
            grid.Add(btn, col, row);
        }
        return grid;
    }

    // сервисный ряд: забой / смена раскладки / смена поля
    private View BuildServiceRow()
    {
        var grid = new Grid { ColumnSpacing = 2, RowSpacing = 2, Margin = new Thickness(0, 2, 0, 0) };
        for (int c = 0; c < 6; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

        var back = new Button
        {
            Text = "\ue14a",                 // backspace (стереть символ)
            FontFamily = "MaterialSymbols",
            Style = GetStyle("TvKey")
        };
        back.Clicked += OnBackspace;
        grid.Add(back, 0, 0);

        var layout = new Button { Text = _lettersLayout ? "123" : "ABC", Style = GetStyle("TvKey") };
        layout.Clicked += OnToggleLayout;
        grid.Add(layout, 1, 0);

        var next = new Button { Style = GetStyle("TvKey") };
        ApplyNextFieldContent(next);
        next.Clicked += OnNextField;
        Grid.SetColumnSpan(next, 4);
        next.HorizontalOptions = LayoutOptions.Fill;
        grid.Add(next, 2, 0);
        _nextFieldButton = next;

        return grid;
    }

    // ===== Ввод =====
    private void OnKey(object? sender, EventArgs e)
    {
        if (sender is Button b) AppendToActiveField(b.Text);
    }

    private void AppendToActiveField(string s)
    {
        if (_editingContract) _vm.Contract += s;
        else _vm.Password += s;
        RefreshFieldTexts();
    }

    private void OnBackspace(object? sender, EventArgs e)
    {
        if (_editingContract && _vm.Contract.Length > 0)
            _vm.Contract = _vm.Contract[..^1];
        else if (!_editingContract && _vm.Password.Length > 0)
            _vm.Password = _vm.Password[..^1];
        RefreshFieldTexts();
    }

    // ABC <-> 123
    private void OnToggleLayout(object? sender, EventArgs e)
    {
        _lettersLayout = !_lettersLayout;
        BuildKeyboard();
        // вернуть фокус на первую клавишу новой раскладки
        Dispatcher.Dispatch(() =>
        {
            var first = KeyboardHost.GetVisualTreeDescendants().OfType<Button>().FirstOrDefault();
            first?.Focus();
        });
    }

    private void OnNextField(object? sender, EventArgs e)
    {
        _editingContract = !_editingContract;
        UpdateFieldHighlight();
        UpdateNextFieldLabel();
    }

    private void UpdateNextFieldLabel()
    {
        if (_nextFieldButton is not null)
            ApplyNextFieldContent(_nextFieldButton);
    }

    // иконка цели (через ImageSource) + текст: к паролю -> замок, к договору -> badge
    private void ApplyNextFieldContent(Button btn)
    {
        // _editingContract=true -> сейчас договор, кнопка ведёт К ПАРОЛЮ (замок)
        string glyph = _editingContract ? "\ue899" : "\uea67"; // lock(пароль) : badge(договор)

        btn.ImageSource = new FontImageSource
        {
            FontFamily = "MaterialSymbols",
            Glyph = glyph,
            Size = 18,
            Color = Colors.White
        };
        btn.ContentLayout = new Button.ButtonContentLayout(
            Button.ButtonContentLayout.ImagePosition.Left, 8);
        btn.Text = _editingContract ? "Пароль" : "Договор";        
    }

    private void RefreshFieldTexts()
    {
        ContractLabel.Text = string.IsNullOrEmpty(_vm.Contract) ? "—" : _vm.Contract;
        PasswordLabel.Text = string.IsNullOrEmpty(_vm.Password)
            ? "Перейдите вниз, чтобы ввести"
            : new string('•', _vm.Password.Length);
        PasswordLabel.TextColor = string.IsNullOrEmpty(_vm.Password)
            ? Color.FromArgb("#5B6B82") : Colors.White;
    }

    private void UpdateFieldHighlight()
    {
        ContractField.Stroke = _editingContract
            ? Color.FromArgb("#1B66E5") : Color.FromArgb("#23303F");
        PasswordField.Stroke = !_editingContract
            ? Color.FromArgb("#1B66E5") : Color.FromArgb("#23303F");
    }

    private async void OnLogin(object? sender, EventArgs e)
    {
        if (_vm.LoginCommand.CanExecute(null))
            await _vm.LoginCommand.ExecuteAsync(null);
    }
}