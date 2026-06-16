using CommunityToolkit.Maui;
using GorodTv.Tv.Pages;
using GorodTv.Tv.Services;
using GorodTV.Core.Services;
using GorodTV.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace GorodTv.Tv
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

            builder.Services.AddSingleton<IDialogService, DialogService>();
            builder.Services.AddSingleton<IGorodTvService, GorodTvService>();
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<ISessionStore, SessionStore>();
            builder.Services.AddSingleton<IAppSettings, AppSettings>();
            builder.Services.AddSingleton<IFavoritesStore, FavoritesStore>();
            builder.Services.AddSingleton(sp => new HttpClient
            {
                BaseAddress = new Uri("https://gorod.tv"),
                Timeout = TimeSpan.FromSeconds(20)
            });

                        
            builder.Services.AddTransient<LoginTvPage>();
            builder.Services.AddTransient<CategoriesTvPage>();


            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<CategoriesViewModel>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
