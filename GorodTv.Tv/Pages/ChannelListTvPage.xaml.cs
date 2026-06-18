using GorodTv.Tv.Controls;
using GorodTV.Core.Models;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class ChannelListTvPage : ContentPage
{
    private readonly ChannelListViewModel _vm;

    private const double CardWidth = 236;
    private const double PreviewHeight = 116;

    public ChannelListTvPage(ChannelListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ChannelListViewModel.IsBusy) && !_vm.IsBusy)
                BuildCards();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BuildCards();
    }

    private void BuildCards()
    {
        LayoutCardsInGrid(_vm.Channels);

        if (_vm.Channels.Count > 0)
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

            var card = BuildCard(ch);
            ChannelsHost.Add(card, col, row);
            i++;
        }
    }

    private View BuildCard(ChannelItem ch)
        => TvChannelCard.Build(ch, CardWidth, PreviewHeight, OnChannelClicked);

    private async void OnChannelClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChannelItem ch })
            await Shell.Current.GoToAsync($"player?id={ch.Id}&name={Uri.EscapeDataString(ch.Name)}");
    }


}
