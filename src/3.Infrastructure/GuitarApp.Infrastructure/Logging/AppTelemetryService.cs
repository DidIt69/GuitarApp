#region Imports
using GuitarApp.Core.Services;
using System.Diagnostics;
#endregion

namespace GuitarApp.Infrastructure.Logging;

public class AppTelemetryService : ITelemetryService
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void LogInformation(string m) => Debug.WriteLine("[INFO] " + m);

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public void LogError(string m, Exception ex) => Debug.WriteLine("[ERROR] " + m + " - " + ex.Message);
}
