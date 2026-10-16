using SQLite;
using System;

namespace GuitarApp.Core.Models;

[Table("DetectedNoteLogs")]
public class DetectedNoteLog
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Indexed]
    [Column("session_id")]
    public int SessionId { get; set; }

    [Column("note_name")]
    [MaxLength(20)]
    public string NoteName { get; set; } = string.Empty;

    [Column("frequency")]
    public double Frequency { get; set; }

    [Column("cents_error")]
    public double CentsError { get; set; }

    [Column("accuracy_score")]
    public double AccuracyScore { get; set; }

    [Column("is_in_tune")]
    public bool IsInTune { get; set; }

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
