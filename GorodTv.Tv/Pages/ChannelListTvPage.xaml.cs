using GorodTV.Core.Models;
using GorodTV.Core.ViewModels;

namespace GorodTv.Tv.Pages;

public partial class ChannelListTvPage : ContentPage
{
    private readonly ChannelListViewModel _vm;

    public ChannelListTvPage(ChannelListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

#if ANDROID
        ChannelsView.HandlerChanged += OnChannelsHandlerChanged;
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // при каждом открытии категории — поставить фокус на первый канал
        FocusFirstCard();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (_vm as ChannelListViewModel)?.CancelEpg();
    }

    // ставит фокус на первую карточку списка (после отрисовки)
    private void FocusFirstCard()
    {
#if ANDROID
        Dispatcher.Dispatch(async () =>
        {
            // ждём, пока CollectionView материализует первую карточку
            for (int attempt = 0; attempt < 20; attempt++)
            {
                await Task.Delay(120);
                if (ChannelsView.Handler?.PlatformView is not
                    AndroidX.RecyclerView.Widget.RecyclerView rv) continue;
                if (rv.ChildCount == 0) continue;

                // первый видимый элемент (верхний-левый)
                var firstChild = rv.GetChildAt(0);
                if (firstChild is null) continue;

                // найти фокусируемую кнопку внутри первой карточки и сфокусировать
                var focusable = FindFocusable(firstChild);
                if (focusable is not null)
                {
                    focusable.RequestFocus();
                    return;
                }
            }
        });
#endif
    }

#if ANDROID
    // рекурсивно ищем первый фокусируемый View внутри карточки
    private static Android.Views.View? FindFocusable(Android.Views.View v)
    {
        if (v.Focusable && v.Visibility == Android.Views.ViewStates.Visible)
            return v;
        if (v is Android.Views.ViewGroup g)
        {
            for (int i = 0; i < g.ChildCount; i++)
            {
                var found = FindFocusable(g.GetChildAt(i)!);
                if (found is not null) return found;
            }
        }
        return null;
    }

    private void OnChannelsHandlerChanged(object? sender, EventArgs e)
    {
        if (ChannelsView.Handler?.PlatformView is AndroidX.RecyclerView.Widget.RecyclerView rv)
        {
            rv.SetItemViewCacheSize(20);
            rv.PreserveFocusAfterLayout = true;

            // подкрутка вниз, когда фокус у нижней кромки (чтобы не уходил на сайдбар)
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
