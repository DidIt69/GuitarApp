using SQLite;
using System;

namespace GuitarApp.Core.Models;

[Table("GuitarSessions")]
public class GuitarSession
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Column("date_session")]
    public DateTime DateSession { get; set; } = DateTime.Now;

    [Column("title")]
    [MaxLength(100)]
    public string Title { get; set; } = "Session d'entraînement";
}
