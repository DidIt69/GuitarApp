#region Imports
using SQLite;
#endregion

namespace GuitarApp.Core.Models;

[Table("GuitarSessions")]
public class GuitarSession
{
    /// <summary>
    /// Obtient ou définit l'identifiant de la sessions
    /// </summary>
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// Obtient ou définit la date de la session
    /// </summary>
    [Column("date_session")]
    public DateTime DateSession { get; set; } = DateTime.Now;

    /// <summary>
    /// Obtient ou définit le nom de la session
    /// </summary>
    [Column("title")]
    [MaxLength(100)]
    public string Title { get; set; } = "Session d'entraînement";
}
