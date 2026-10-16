using Xunit;
using System;
using GuitarApp.Infrastructure.Audio;

namespace GuitarApp.UnitTests;

public class YinPitchAlgorithmTests
{
    private const int SampleRate = 44100;
    private const int WindowSize = 2048;
    private const double ToleranceHz = 1.5; // Tolérance légèrement élargie à 1.5 Hz pour absorber le bruit complexe

    /// <summary>
    /// TEST DE BRUIT : L'algorithme doit détecter la note LA (440 Hz) même si elle est polluée par un bruit blanc ambiant.
    /// </summary>
    [Fact]
    public void ComputeYinPitch_With440HzSignalAndWhiteNoise_ShouldStillReturn440Hz()
    {
        // Arrange : Signal pur à 440 Hz
        float[] audioBuffer = GeneratePureSineWave(440.0, SampleRate, WindowSize);
        
        // Ajout d'un bruit blanc significatif (20% de l'amplitude totale du signal)
        // Simule une pièce avec un ventilateur, un ordinateur ou une discussion en fond
        AddWhiteNoise(audioBuffer, noiseLevel: 0.20f);
        
        var pitchService = new PublicPitchDetectionService();

        // Act
        double detectedPitch = pitchService.ExposeComputeYinPitch(audioBuffer, SampleRate, 0.15);

        // Assert : L'algorithme YIN doit résister au bruit grâce à sa normalisation cumulative
        Assert.True(Math.Abs(440.0 - detectedPitch) <= ToleranceHz, 
            $"Résistance au bruit échouée. Pitch détecté : {detectedPitch} Hz au lieu de 440 Hz.");
    }

    /// <summary>
    /// TEST DE HARMONIQUES : Simule la physique réelle d'une guitare. 
    /// Quand on joue un LA (110 Hz), la corde produit aussi du 220 Hz (octave) et du 330 Hz (quinte) parfois plus forts que la fondamentale.
    /// </summary>
    [Fact]
    public void ComputeYinPitch_WithComplexGuitarHarmonics_ShouldFindFundamentalNotHarmonics()
    {
        // Arrange : Construction d'un signal guitare réaliste (Fondatale à 110 Hz + Harmoniques)
        float[] audioBuffer = new float[WindowSize];
        var random = new Random();

        for (int i = 0; i < WindowSize; i++)
        {
            double t = (double)i / SampleRate;
            
            double fundamental = Math.Sin(2 * Math.PI * 110.0 * t);      // Note visée (La 2)
            double harmonic1 = 0.7 * Math.Sin(2 * Math.PI * 220.0 * t);   // Octave supérieure
            double harmonic2 = 0.5 * Math.Sin(2 * Math.PI * 330.0 * t);   // Quinte supérieure
            
            // Fusion des ondes dans le tampon audio
            audioBuffer[i] = (float)(fundamental + harmonic1 + harmonic2);
        }

        // Normalisation automatique entre -1.0 et 1.0 pour éviter la saturation du signal
        NormalizeBuffer(audioBuffer);

        var pitchService = new PublicPitchDetectionService();

        // Act
        double detectedPitch = pitchService.ExposeComputeYinPitch(audioBuffer, SampleRate, 0.15);

        // Assert : Une simple FFT échouerait souvent ici en choisissant le 220 Hz. 
        // L'algorithme YIN doit impérativement trouver la fondamentale (110 Hz).
        Assert.True(Math.Abs(110.0 - detectedPitch) <= ToleranceHz, 
            $"Erreur d'octave détectée. L'algorithme a renvoyé {detectedPitch} Hz au lieu de la fondamentale à 110 Hz.");
    }

    /// <summary>
    /// TEST AUX LIMITES (ATTÉNUATION) : Vérifie le comportement lorsque la note s'éteint (Sustain de fin de note).
    /// </summary>
    [Fact]
    public void ComputeYinPitch_WithVeryWeakSignal_ShouldReturnNegativeOne()
    {
        // Arrange : Signal à 440 Hz mais d'une amplitude extrêmement faible (proche du silence numérique numérique)
        float[] audioBuffer = GeneratePureSineWave(440.0, SampleRate, WindowSize);
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            audioBuffer[i] *= 0.001f; // Atténuation massive (99.9% de volume en moins)
        }

        var pitchService = new PublicPitchDetectionService();

        // Act
        double detectedPitch = pitchService.ExposeComputeYinPitch(audioBuffer, SampleRate, 0.15);

        // Assert : En dessous d'un certain seuil d'énergie, l'application doit ignorer le signal pour éviter d'afficher des notes instables.
        Assert.Equal(-1, detectedPitch);
    }

    #region Outils d'ingénierie audio (Helpers)

    private float[] GeneratePureSineWave(double frequency, int sampleRate, int size)
    {
        float[] buffer = new float[size];
        for (int i = 0; i < size; i++)
        {
            buffer[i] = (float)Math.Sin(2 * Math.PI * frequency * i / sampleRate);
        }
        return buffer;
    }

    private void AddWhiteNoise(float[] buffer, float noiseLevel)
    {
        var random = new Random();
        for (int i = 0; i < buffer.Length; i++)
        {
            // Génération d'une valeur de bruit comprise entre -noiseLevel et +noiseLevel
            float noise = (float)(random.NextDouble() * 2.0 - 1.0) * noiseLevel;
            buffer[i] += noise;
        }
        NormalizeBuffer(buffer);
    }

    private void NormalizeBuffer(float[] buffer)
    {
        float max = 0f;
        foreach (var sample in buffer)
        {
            if (Math.Abs(sample) > max) max = Math.Abs(sample);
        }
        if (max > 0)
        {
            for (int i = 0; i < buffer.Length; i++) buffer[i] /= max;
        }
    }

    #endregion
}

public class PublicPitchDetectionService : PitchDetectionService
{
    public double ExposeComputeYinPitch(float[] buffer, int sampleRate, double threshold)
    {
        var method = typeof(PitchDetectionService).GetMethod("ComputeYinPitch", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null) throw new InvalidOperationException("La méthode ComputeYinPitch est introuvable.");
        
        return (double)method.Invoke(this, new object[] { buffer, sampleRate, threshold })!;
    }
}
