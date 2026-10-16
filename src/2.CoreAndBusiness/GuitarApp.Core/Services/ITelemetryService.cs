namespace GuitarApp.Core.Services;

public interface ITelemetryService
{
    /// <summary>
    /// Méthode permettant de logger des informations
    /// </summary>
    /// <param name="message"></param>
    void LogInformation(string message);

    /// <summary>
    /// Méthode permettant de logger des erreurs
    /// </summary>
    /// <param name="message"></param>
    /// <param name="ex"></param>
    void LogError(string message, Exception ex);
}
