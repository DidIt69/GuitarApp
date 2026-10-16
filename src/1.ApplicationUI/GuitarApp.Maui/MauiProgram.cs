#region Imports
using GuitarApp.Core.Services;
using GuitarApp.Core.ViewModels;
using GuitarApp.Infrastructure.Audio;
using GuitarApp.Infrastructure.Data;
using GuitarApp.Infrastructure.Logging;
using GuitarApp.Maui.Views;
#endregion

namespace GuitarApp.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddSingleton<ITelemetryService, AppTelemetryService>();
        builder.Services.AddSingleton<IPitchDetectionService, PitchDetectionService>();
        builder.Services.AddSingleton<ITuningEvaluator, TuningEvaluator>();
        builder.Services.AddSingleton(typeof(IRepository<>), typeof(SQLiteRepository<>));

        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainView>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<HistoryView>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
