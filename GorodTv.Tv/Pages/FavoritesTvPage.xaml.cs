using GorodTV.Core.Models;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class FavoritesTvPage : ContentPage
{
    private readonly FavoritesViewModel _vm;

    public FavoritesTvPage(FavoritesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

#if ANDROID
        ChannelsView.HandlerChanged += OnChannelsHandlerChanged;
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
        FocusFirstCard();
    }

    private void FocusFirstCard()
    {
#if ANDROID
        Dispatcher.Dispatch(async () =>
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                await Task.Delay(120);
                if (ChannelsView.Handler?.PlatformView is not
                    AndroidX.RecyclerView.Widget.RecyclerView rv) continue;
                if (rv.ChildCount == 0) continue;
                var firstChild = rv.GetChildAt(0);
                if (firstChild is null) continue;
                var focusable = FindFocusable(firstChild);
                if (focusable is not null) { focusable.RequestFocus(); return; }
            }
        });
#endif
    }

#if ANDROID
    private static Android.Views.View? FindFocusable(Android.Views.View v)
    {
        if (v.Focusable && v.Visibility == Android.Views.ViewStates.Visible)
            return v;
        if (v is Android.Views.ViewGroup g)
            for (int i = 0; i < g.ChildCount; i++)
            {
                var found = FindFocusable(g.GetChildAt(i)!);
                if (found is not null) return found;
            }
        return null;
    }

    private void OnChannelsHandlerChanged(object? sender, EventArgs e)
    {
        if (ChannelsView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView rv)
        {
            rv.SetItemViewCacheSize(20);
            rv.PreserveFocusAfterLayout = true;
            var vto = rv.ViewTreeObserver;
            if (vto is null)
            {
                return;
            }
            vto.GlobalFocusChange += (s, a) =>
            {
                var focused = a.NewFocus;
                if (focused is null) return;
                var loc = new int[2];
                focused.GetLocationOnScreen(loc);
                var rvLoc = new int[2];
                rv.GetLocationOnScreen(rvLoc);
                int focusBottom = loc[1] + focused.Height;
                int rvBottom = rvLoc[1] + rv.Height;
                if (focusBottom > rvBottom - focused.Height)
                    rv.SmoothScrollBy(0, focused.Height + 16);
            };
        }
    }
#endif

    private async void OnChannelClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ChannelItem ch })
            await Shell.Current.GoToAsync($"player?id={ch.Id}&name={Uri.EscapeDataString(ch.Name)}");
    }
}
