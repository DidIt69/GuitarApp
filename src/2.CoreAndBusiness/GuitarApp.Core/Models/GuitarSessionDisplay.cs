namespace GuitarApp.Core.Models;

public class GuitarSessionDisplay
{
    /// <summary>
    /// Obtient ou définit l'identifiant de la session
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Obtient ou définit la date de la session
    /// </summary>
    public DateTime DateSession { get; set; }

    /// <summary>
    /// Obtient ou définit le titre de la session
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Obtient ou définit la moyenne
    /// </summary>
    public double AverageAccuracy { get; set; }

    /// <summary>
    /// Obtient ou déifnit le nombre total de note
    /// </summary>
    public int TotalNotesCount { get; set; }

    /// <summary>
    /// Obtient ou définit la couleur de la performance
    /// </summary>
    public string PerformanceColor { get; set; } = "#808080";
}
