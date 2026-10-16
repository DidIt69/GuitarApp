using System;

namespace GuitarApp.Core.Models;

public class GuitarSessionDisplay
{
    public int Id { get; set; }
    public DateTime DateSession { get; set; }
    public string Title { get; set; } = string.Empty;
    public double AverageAccuracy { get; set; }
    public int TotalNotesCount { get; set; }
    public string PerformanceColor { get; set; } = "#808080";
}
