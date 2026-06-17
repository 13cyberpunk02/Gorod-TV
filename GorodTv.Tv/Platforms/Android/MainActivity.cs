using Android.App;
using Android.Content.PM;
using Android.OS;

namespace GorodTv.Tv;

[Activity(Theme = "@style/Maui.SplashTheme", 
    MainLauncher = true,
    Exported = true,
    LaunchMode = LaunchMode.SingleTop, 
    ConfigurationChanges = ConfigChanges.ScreenSize         | 
                           ConfigChanges.Orientation        | 
                           ConfigChanges.UiMode             | 
                           ConfigChanges.ScreenLayout       | 
                           ConfigChanges.SmallestScreenSize | 
                           ConfigChanges.Density)]

[IntentFilter([Android.Content.Intent.ActionMain],Categories = new[] {"android.intent.category.LEANBACK_LAUNCHER",Android.Content.Intent.CategoryLauncher})]
public class MainActivity : MauiAppCompatActivity
{
    public static event Action<string>? RemoteKey;
    
    // когда плеер на seek-баре — гасим ←→, чтобы фокус не уезжал на соседние кнопки
    public static bool SwallowHorizontalKeys = false;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);     
    }


    public override bool DispatchKeyEvent(Android.Views.KeyEvent? e)
    {
        if (e is not null && e.Action == Android.Views.KeyEventActions.Down)
        {
            string? key = e.KeyCode switch
            {
                Android.Views.Keycode.DpadCenter or Android.Views.Keycode.Enter => "DpadCenter",
                Android.Views.Keycode.DpadLeft => "DpadLeft",
                Android.Views.Keycode.DpadRight => "DpadRight",
                Android.Views.Keycode.DpadUp => "DpadUp",
                Android.Views.Keycode.DpadDown => "DpadDown",
                _ => null
            };
            if (key is not null)
            {
                RemoteKey?.Invoke(key);

                // если плеер на seek-баре и это ←→ — ПОГЛОЩАЕМ событие (фокус не уедет)
                if (SwallowHorizontalKeys && (key == "DpadLeft" || key == "DpadRight"))
                    return true;   // не передаём дальше -> система не двигает фокус
            }
        }
        return base.DispatchKeyEvent(e);
    }
}
