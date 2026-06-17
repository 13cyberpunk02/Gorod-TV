using GorodTv.Tv.Controls;
using GorodTV.Core.Models;
using GorodTV.Core.Services;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class FavoritesTvPage : ContentPage
{
    private readonly FavoritesViewModel _vm;
    private readonly IGorodTvService _tv;
    private readonly IDialogService _dialogs;

    private const double CardWidth = 236;
    private const double PreviewHeight = 116;

    public FavoritesTvPage(FavoritesViewModel vm, IGorodTvService tv, IDialogService dialogs)
	{
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _tv = tv;
        _dialogs = dialogs;

        _vm.Channels.CollectionChanged += (_, _) => BuildCards();
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
        BuildCards();
    }

    private void BuildCards()
    {
        ChannelsHost.Children.Clear();
        foreach (var ch in _vm.Channels)
            ChannelsHost.Children.Add(TvChannelCard.Build(ch, CardWidth, PreviewHeight, OnChannelClicked));

        bool empty = _vm.Channels.Count == 0;
        EmptyState.IsVisible = empty;

        if (!empty)
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(150);
                var first = ChannelsHost.GetVisualTreeDescendants().OfType<Button>().FirstOrDefault();
                first?.Focus();
            });
    }

    private async void OnChannelClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChannelItem ch })
            await Shell.Current.GoToAsync($"player?id={ch.Id}&name={Uri.EscapeDataString(ch.Name)}");
    }

    private async void OnNavCategories(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//categories");

    private void OnNavFavorites(object? sender, EventArgs e) { /* уже здесь */ }

    private async void OnLogout(object? sender, EventArgs e)
    {
        bool ok = await _dialogs.ConfirmAsync(
            "Выйти из аккаунта?",
            "Нужно будет снова ввести номер договора и пароль.",
            "Выйти", "Отмена", AlertKind.Info);
        if (!ok) return;
        _tv.Logout();
        await Shell.Current.GoToAsync("//login");
    }
}