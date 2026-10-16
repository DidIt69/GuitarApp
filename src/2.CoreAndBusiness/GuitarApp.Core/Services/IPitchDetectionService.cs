using GuitarApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GuitarApp.Core.Services;

public interface IPitchDetectionService
{
    event Action<double> PitchDetected;
    event Action<List<double>> ChordPitchesDetected;
    event Action<float[]> WaveformReceived;
    Task StartListeningAsync(AudioSourceType sourceType);
    Task StopListeningAsync();
}
