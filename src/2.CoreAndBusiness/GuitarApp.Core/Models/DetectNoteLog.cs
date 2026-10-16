#region Imports
using SQLite;
#endregion

namespace GuitarApp.Core.Models;

[Table("DetectedNoteLogs")]
public class DetectedNoteLog
{
    /// <summary>
    /// Obtient ou définit l'identifiant de la note
    /// </summary>
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Obtient ou définit l'identifiant de la session
    /// </summary>
    [Indexed]
    [Column("session_id")]
    public int SessionId { get; set; }

    /// <summary>
    /// Obtient ou définit le nom de la note
    /// </summary>
    [Column("note_name")]
    [MaxLength(20)]
    public string NoteName { get; set; } = string.Empty;

    /// <summary>
    /// Obtient ou définit la fréquence de la note
    /// </summary>
    [Column("frequency")]
    public double Frequency { get; set; }

    /// <summary>
    /// Obtient ou définit le niveau de l'erreur
    /// </summary>
    [Column("cents_error")]
    public double CentsError { get; set; }

    /// <summary>
    /// Obtient ou définit le score de la note
    /// </summary>
    [Column("accuracy_score")]
    public double AccuracyScore { get; set; }

    /// <summary>
    /// Obtient ou définit si on est dans la music
    /// </summary>
    [Column("is_in_tune")]
    public bool IsInTune { get; set; }

    /// <summary>
    /// Obtient ou définit
    /// </summary>
    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
