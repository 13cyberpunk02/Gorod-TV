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
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);     
    }   
}
