using GuitarApp.Core.Models;

namespace GuitarApp.Core.Services;

public interface ITuningEvaluator
{
    TuningResult EvaluatePitch(double measuredFrequency);
}
