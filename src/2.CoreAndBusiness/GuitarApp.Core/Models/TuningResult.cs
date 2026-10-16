using System;

namespace GuitarApp.Core.Models;

public class TuningResult
{
    public string NoteName { get; set; } = string.Empty;
    public double TargetFrequency { get; set; }
    public double MeasuredFrequency { get; set; }
    public double CentsError { get; set; }
    public double AccuracyScore { get; set; }
    public bool IsInTune { get; set; }
}
