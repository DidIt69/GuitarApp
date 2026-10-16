#region Imports
using GuitarApp.Core.Models;
#endregion

namespace GuitarApp.Core.Services;

public interface ITuningEvaluator
{
    /// <summary>
    /// Méthode permettant d'évaluer une fréqquence
    /// </summary>
    /// <param name="measuredFrequency"></param>
    /// <returns></returns>
    TuningResult EvaluatePitch(double measuredFrequency);
}
