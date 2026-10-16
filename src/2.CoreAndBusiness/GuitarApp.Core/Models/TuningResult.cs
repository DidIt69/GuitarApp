namespace GuitarApp.Core.Models;

public class TuningResult
{
    /// <summary>
    /// Obtient ou définit le nom de la note
    /// </summary>
    public string NoteName { get; set; } = string.Empty;

    /// <summary>
    /// Obtient ou définit la fréquence cible
    /// </summary>
    public double TargetFrequency { get; set; }

    /// <summary>
    /// Obtient ou définit la fréquence mesurée
    /// </summary>
    public double MeasuredFrequency { get; set; }

    /// <summary>
    /// Obtient ou définit le niveau d'erreur
    /// </summary>
    public double CentsError { get; set; }

    /// <summary>
    /// Obtient ou définit le score
    /// </summary>
    public double AccuracyScore { get; set; }

    /// <summary>
    /// Obtient ou définit si c'est une musique
    /// </summary>
    public bool IsInTune { get; set; }
}
