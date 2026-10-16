using GuitarApp.Core.Services;
using GuitarApp.Core.ViewModels;
using GuitarApp.Infrastructure.Audio;
using GuitarApp.Infrastructure.Data;
using GuitarApp.Infrastructure.Logging;
using GuitarApp.Maui.Views;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace GuitarApp.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Injection IoC des Services Métiers & d'Infrastructure
        builder.Services.AddSingleton<ITelemetryService, AppTelemetryService>();
        builder.Services.AddSingleton<IPitchDetectionService, PitchDetectionService>();
        builder.Services.AddSingleton<ITuningEvaluator, TuningEvaluator>();
        builder.Services.AddSingleton(typeof(IRepository<>), typeof(SQLiteRepository<>));

        // Enregistrement des MVVM Transients
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainView>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<HistoryView>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
