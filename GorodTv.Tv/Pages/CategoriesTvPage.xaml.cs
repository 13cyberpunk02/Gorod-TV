using GorodTV.Core.Models;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class CategoriesTvPage : ContentPage
{
    private readonly CategoriesViewModel _vm;
    private bool _focusedOnce;
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

        // КРИТИЧНО для ТВ: задать стартовый фокус, иначе пульт «не знает», откуда начать.
        // Фокусируем первую карточку после построения списка.
        if (!_focusedOnce)
        {
            _focusedOnce = true;
            await Task.Delay(300); // дать UI построиться
            FocusFirstCard();
        }
    }

    private void FocusFirstCard()
    {
        // ищем первую кнопку-карточку в визуальном дереве и фокусируем
        var firstButton = this.GetVisualTreeDescendants()
            .OfType<Button>()
            .FirstOrDefault();
        firstButton?.Focus();
    }

    // OK по карточке -> открыть список каналов категории (та же навигация, что в Mobile)
    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CategoryItem cat })
        {
            await Shell.Current.GoToAsync(
                $"channels?category={Uri.EscapeDataString(cat.Id)}&title={Uri.EscapeDataString(cat.Title)}");
        }
    }
}