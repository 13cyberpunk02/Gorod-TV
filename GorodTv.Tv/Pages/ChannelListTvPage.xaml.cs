using GorodTv.Tv.Controls;
using GorodTV.Core.Models;
using GorodTV.Core.Services;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class ChannelListTvPage : ContentPage
{
    private readonly ChannelListViewModel _vm;
    private readonly IGorodTvService _tv;
    private readonly IDialogService _dialogs;

    private const double CardWidth = 236;
    private const double PreviewHeight = 116;


    public ChannelListTvPage(ChannelListViewModel vm, IGorodTvService tv, IDialogService dialogs)
	{
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
        _tv = tv;
        _dialogs = dialogs;

        // перестроить карточки, когда загрузка завершится (IsBusy -> false)
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChannelListViewModel.IsBusy) && !_vm.IsBusy)
                BuildCards();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // если данные уже есть — построить сразу; иначе построятся по завершении загрузки
        BuildCards();
    }

    private void BuildCards()
    {
        ChannelsHost.Children.Clear();

        // ВРЕМЕННАЯ ДИАГНОСТИКА: показать число каналов и CategoryId
        ChannelsHost.Children.Add(new Label
        {
            Text = $"DEBUG: каналов={_vm.Channels.Count}, cat={_vm.CategoryId}, busy={_vm.IsBusy}",
            TextColor = Colors.Yellow,
            FontSize = 12
        });

        foreach (var ch in _vm.Channels)
            ChannelsHost.Children.Add(BuildCard(ch));

        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(150);
            var first = ChannelsHost.GetVisualTreeDescendants().OfType<Button>().FirstOrDefault();
            first?.Focus();
        });
    }

    private View BuildCard(ChannelItem ch)
        => TvChannelCard.Build(ch, CardWidth, PreviewHeight, OnChannelClicked);

    private async void OnChannelClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChannelItem ch })
            await Shell.Current.GoToAsync($"player?channel={ch.Id}");
    }

    // ===== Сайдбар =====
    private async void OnNavCategories(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//categories");

    private async void OnNavFavorites(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("//favorites");

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