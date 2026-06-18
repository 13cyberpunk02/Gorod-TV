using GorodTv.Tv.Controls;
using GorodTV.Core.Models;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class FavoritesTvPage : ContentPage
{
    private readonly FavoritesViewModel _vm;

    private const double CardWidth = 236;
    private const double PreviewHeight = 116;

    public FavoritesTvPage(FavoritesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

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
        LayoutCardsInGrid(_vm.Channels);

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

    private const int Columns = 3;

    private void LayoutCardsInGrid(IEnumerable<ChannelItem> channels)
    {
        ChannelsHost.Children.Clear();
        ChannelsHost.ColumnDefinitions.Clear();
        ChannelsHost.RowDefinitions.Clear();

        for (int c = 0; c < Columns; c++)
            ChannelsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        int i = 0;
        foreach (var ch in channels)
        {
            int row = i / Columns, col = i % Columns;
            if (col == 0)
                ChannelsHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            ChannelsHost.Add(TvChannelCard.Build(ch, CardWidth, PreviewHeight, OnChannelClicked), col, row);
            i++;
        }
    }

    private async void OnChannelClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChannelItem ch })
            await Shell.Current.GoToAsync($"player?id={ch.Id}&name={Uri.EscapeDataString(ch.Name)}");
    }


}