using Microsoft.Extensions.Logging;
using LentSoft.Mobile.Services;
using LentSoft.Mobile.ViewModels;
using LentSoft.Mobile.Views;

namespace LentSoft.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── Client Services ──
        builder.Services.AddSingleton<IAuthStorageService, AuthStorageService>();
        builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

        string baseUrl = (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType == DeviceType.Virtual)
            ? "http://10.0.2.2:5000/"
            : "http://localhost:5000/";

        builder.Services.AddHttpClient<IApiService, ApiService>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

        // ── ViewModels ──
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<InicioViewModel>();
        builder.Services.AddTransient<TiendaViewModel>();
        builder.Services.AddTransient<FacturasViewModel>();
        builder.Services.AddTransient<CitasViewModel>();
        builder.Services.AddTransient<AgendarCitaViewModel>();
        builder.Services.AddTransient<PerfilViewModel>();

        // ── Views ──
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<InicioPage>();
        builder.Services.AddTransient<TiendaPage>();
        builder.Services.AddTransient<FacturasPage>();
        builder.Services.AddTransient<CitasPage>();
        builder.Services.AddTransient<AgendarCitaModal>();
        builder.Services.AddTransient<PerfilPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
