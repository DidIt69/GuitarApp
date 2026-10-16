using GuitarApp.Core.Services;
using System;
using System.Diagnostics;

namespace GuitarApp.Infrastructure.Logging;

public class AppTelemetryService : ITelemetryService
{
    public void LogInformation(string m) => Debug.WriteLine("[INFO] " + m);
    public void LogError(string m, Exception ex) => Debug.WriteLine("[ERROR] " + m + " - " + ex.Message);
}
