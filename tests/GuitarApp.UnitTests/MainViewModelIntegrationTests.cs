using Xunit;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using GuitarApp.Core.ViewModels;
using GuitarApp.Core.Models;
using GuitarApp.Core.Services;
using GuitarApp.Infrastructure.Audio;

namespace GuitarApp.UnitTests;

public class MainViewModelIntegrationTests
{
    private const int SampleRate = 44100;
    private const int WindowSize = 2048;

    [Fact]
    public async Task WhenValidGuitarStringIsPlayed_ViewModelShouldDetectAndLogCorrectNote()
    {
        // 1. ARRANGE (Configuration des Mocks pour l'IoC)
        var mockTelemetry = new Mock<ITelemetryService>();
        var mockSessionRepo = new Mock<IRepository<GuitarSession>>();
        var mockNoteRepo = new Mock<IRepository<DetectedNoteLog>>();

        // Utilisation du vrai évaluateur de justesse (logique métier)
        ITuningEvaluator tuningEvaluator = new TuningEvaluator();

        // Utilisation du vrai service YIN (infrastructure)
        var pitchService = new PitchDetectionService();

        // Instanciation du ViewModel via injection (simule le conteneur IoC)
        var viewModel = new MainViewModel(
            mockTelemetry.Object,
            pitchService,
            tuningEvaluator,
            mockSessionRepo.Object,
            mockNoteRepo.Object);

        // Simulation : L'utilisateur clique sur "Démarrer la session"
        await viewModel.ToggleListeningCommand.ExecuteAsync(null);

        // Génération en mémoire du signal PCM d'une corde de LA parfaite (110.00 Hz)
        float[] audioBuffer = GenerateSineWave(110.00, SampleRate, WindowSize);

        // 2. ACT (On injecte manuellement le tampon audio dans l'algorithme YIN)
        // Utilisation de la réflexion pour simuler l'arrivée des données de la carte son
        var method = typeof(PitchDetectionService).GetMethod("ComputeYinPitch",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        double detectedPitch = (double)method!.Invoke(pitchService, new object[] { audioBuffer, SampleRate, 0.15 })!;

        // On lève manuellement l'événement du service pour propager la fréquence au ViewModel
        var raiseEventMethod = pitchService.GetType().GetEvent("PitchDetected");
        var eventField = pitchService.GetType().GetField("PitchDetected",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var eventDelegate = (Action<double>?)eventField?.GetValue(pitchService);

        eventDelegate?.Invoke(detectedPitch);

        // 3. ASSERT (Vérifications des états de l'UI et de la persistance)

        // Vérification de la réactivité visuelle (Aiguille et étiquettes)
        Assert.Equal("La (2)", viewModel.DetectedNote);
        Assert.True(Math.Abs(110.00 - viewModel.CurrentFrequency) <= 0.1,
            $"La fréquence détectée ({viewModel.CurrentFrequency} Hz) est hors tolérance.");

        // Les cents d'écart doivent être extrêmement proches de 0 (tolérance de ±0.5 centième de demi-ton)
        Assert.True(Math.Abs(viewModel.CentsError) <= 0.5,
            $"L'erreur en cents ({viewModel.CentsError} cts) est trop élevée pour une note juste.");

        Assert.True(viewModel.AccuracyScore >= 98,
            $"Le score de précision ({viewModel.AccuracyScore}%) est trop bas pour un signal parfait.");

        // Pensez à adapter également la vérification du Mock SQLite plus bas :
        mockNoteRepo.Verify(r => r.SaveAsync(It.Is<DetectedNoteLog>(n =>
            n.NoteName == "La (2)" &&
            n.AccuracyScore >= 98)), // Marge ici aussi
            Times.Once, "La note détectée n'a pas été sauvegardée avec le bon niveau de précision.");
        
        Assert.Equal("#2B7A4B", viewModel.TuningIndicatorColor); // Doit passer au vert

        // Vérification du défilement en direct (La note doit être empilée en haut de la liste)
        Assert.Single(viewModel.LiveSessionNotes);
        Assert.Equal("La (2)", viewModel.LiveSessionNotes[0].NoteName);
        Assert.True(viewModel.LiveSessionNotes[0].AccuracyScore >= 98,
            $"Le score de précision ({viewModel.LiveSessionNotes[0].AccuracyScore}%) est trop bas pour un signal parfait.");

        // Vérification de la persistance SQLite (L'enregistrement en base doit avoir été appelé)
        mockNoteRepo.Verify(r => r.SaveAsync(It.Is<DetectedNoteLog>(n =>
            n.NoteName == "La (2)" &&
            n.AccuracyScore >= 98)),
            Times.Once, "La note détectée n'a pas été sauvegardée dans SQLite.");
    }

    /// <summary>
    /// Helper pour modéliser une onde sonore PCM pure sans fichier physique
    /// </summary>
    private float[] GenerateSineWave(double frequency, int sampleRate, int size)
    {
        float[] buffer = new float[size];
        for (int i = 0; i < size; i++)
        {
            buffer[i] = (float)Math.Sin(2 * Math.PI * frequency * i / sampleRate);
        }
        return buffer;
    }
}
