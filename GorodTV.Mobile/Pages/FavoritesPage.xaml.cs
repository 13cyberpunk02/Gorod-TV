using GorodTV.Core.ViewModels;

namespace GorodTV.Pages;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesViewModel _vm;
    public FavoritesPage(FavoritesViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }
}