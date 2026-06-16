using GorodTV.Core.ViewModels;

namespace GorodTV.Pages;

public partial class CategoriesPage : ContentPage
{
	public CategoriesPage(CategoriesViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CategoriesViewModel vm && vm.Categories.Count == 0)
            vm.LoadCommand.Execute(null);
    }
}