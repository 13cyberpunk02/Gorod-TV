using CommunityToolkit.Maui;
using GorodTV.Core.Services;
using GorodTV.Core.ViewModels;
using GorodTV.Pages;
using GorodTV.Services;
using Microsoft.Extensions.Logging;

namespace GorodTV
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: false)
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Onest-Medium.ttf", "OnestMedium");
                    fonts.AddFont("Onest-Regular.ttf", "Onest");
                    fonts.AddFont("Onest-Bold.ttf", "OnestBold");
                    fonts.AddFont("MaterialSymbolsRounded-Filled.ttf", "MaterialSymbols");
                });
            builder.Services.AddSingleton<IAppSettings, AppSettings>();
            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<ISessionStore, SessionStore>();
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IGorodTvService, GorodTvService>();
            builder.Services.AddSingleton<IFavoritesStore, FavoritesStore>();
            builder.Services.AddSingleton(sp => new HttpClient
            {
                BaseAddress = new Uri("https://gorod.tv"),
                Timeout = TimeSpan.FromSeconds(20)
            });


            builder.Services.AddSingleton<App>();

            // ===== ViewModels =====
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<CategoriesViewModel>();
            builder.Services.AddTransient<ChannelListViewModel>();
            builder.Services.AddTransient<PlayerViewModel>();
            builder.Services.AddTransient<FavoritesViewModel>();


            // ===== Pages (Shell берёт их из DI и сам внедряет VM в конструктор) =====
            builder.Services.AddTransient<SplashPage>();
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<CategoriesPage>();
            builder.Services.AddTransient<ChannelListPage>();
            builder.Services.AddTransient<PlayerPage>();
            builder.Services.AddTransient<FavoritesPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
