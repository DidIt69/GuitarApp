#region Imports
using GuitarApp.Core.Models;
#endregion

namespace GuitarApp.Core.Services;

public interface IPitchDetectionService
{
    /// <summary>
    /// Evenement lorsqu'une note est detecté
    /// </summary>
    event Action<double> PitchDetected;

    /// <summary>
    /// Evenement lorsqu'un accord est detecté
    /// </summary>
    event Action<List<double>> ChordPitchesDetected;

    /// <summary>
    /// Evenement lorsqu'une onde est detectée
    /// </summary>
    event Action<float[]> WaveformReceived;

    /// <summary>
    /// Méthode permettant de lancer l'écoute
    /// </summary>
    /// <param name="sourceType">Type de la source audio a utiliser</param>
    /// <returns></returns>
    Task StartListeningAsync(AudioSourceType sourceType);

    /// <summary>
    /// Methode permettant d'arrêter d'écouter la source
    /// </summary>
    /// <returns></returns>
    Task StopListeningAsync();
}
