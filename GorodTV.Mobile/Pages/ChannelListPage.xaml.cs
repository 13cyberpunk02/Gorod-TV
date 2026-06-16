using GorodTV.Core.ViewModels;

namespace GorodTV.Pages;

public partial class ChannelListPage : ContentPage
{
	public ChannelListPage(ChannelListViewModel vm)
	{
		InitializeComponent();
        BindingContext = vm;
    }
}