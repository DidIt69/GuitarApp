using System;

namespace GuitarApp.Core.Services;

public interface ITelemetryService
{
    void LogInformation(string message);
    void LogError(string message, Exception ex);
}
