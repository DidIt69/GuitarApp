using Xunit;
using System;
using GuitarApp.Infrastructure.Audio;

namespace GuitarApp.UnitTests;

public class YinRobustnessTests
{
    private const int SampleRate = 44100;
    private const int WindowSize = 2048;
    private const double ToleranceHz = 1.0; // Précision chirurgicale exigée (±1 Hz)

    /// <summary>
    /// TEST DE BRUIT : L'algorithme doit détecter un LA standard (A4 = 440 Hz) 
    /// même si le signal est noyé dans 20% de bruit blanc ambiant.
    /// </summary>
    [Fact]
    public void ComputeYinPitch_With440HzSignalAndWhiteNoise_ShouldStillReturn440Hz()
    {
        // Arrange : Génération du signal pur à 440 Hz
        float[] audioBuffer = GeneratePureSineWave(440.0, SampleRate, WindowSize);
        
        // Injection de 20% de bruit blanc (simule un ordinateur, ventilateur ou brouhaha en arrière-plan)
        AddWhiteNoise(audioBuffer, noiseLevel: 0.20f);
        
        var pitchService = new PublicPitchDetectionService();

        // Act
        double detectedPitch = pitchService.ExposeComputeYinPitch(audioBuffer, SampleRate, 0.15);

        // Assert : La normalisation cumulative moyenne de YIN doit filtrer ce bruit
        Assert.True(Math.Abs(440.0 - detectedPitch) <= ToleranceHz, 
            $"Résistance au bruit échouée. Pitch détecté : {detectedPitch} Hz au lieu de 440 Hz.");
    }

    /// <summary>
    /// TEST DE HARMONIQUES : Simule la physique réelle d'une guitare acoustique.
    /// Une corde de LA grave (110 Hz) génère naturellement ses harmoniques (220 Hz, 330 Hz, etc.)
    /// parfois plus puissantes que la fondamentale. L'algorithme doit trouver la fondamentale.
    /// </summary>
    [Fact]
    public void ComputeYinPitch_WithComplexGuitarHarmonics_ShouldFindFundamentalNotOctaves()
    {
        // Arrange : Construction d'un signal guitare composite
        float[] audioBuffer = new float[WindowSize];

        for (int i = 0; i < WindowSize; i++)
        {
            double t = (double)i / SampleRate;
            
            double fundamental = Math.Sin(2 * Math.PI * 110.0 * t);       // Corde jouée : LA (110 Hz)
            double harmonic1 = 0.8 * Math.Sin(2 * Math.PI * 220.0 * t);    // Harmonique 1 (Octave) à 80% d'amplitude
            double harmonic2 = 0.5 * Math.Sin(2 * Math.PI * 330.0 * t);    // Harmonique 2 (Quinte) à 50% d'amplitude
            
            audioBuffer[i] = (float)(fundamental + harmonic1 + harmonic2);
        }

        NormalizeBuffer(audioBuffer);
        var pitchService = new PublicPitchDetectionService();

        // Act
        double detectedPitch = pitchService.ExposeComputeYinPitch(audioBuffer, SampleRate, 0.15);

        // Assert : Une FFT classique choisirait le pic à 220Hz. YIN doit cibler 110Hz.
        Assert.True(Math.Abs(110.0 - detectedPitch) <= ToleranceHz, 
            $"Erreur d'octave. L'algorithme s'est fait piéger par les harmoniques et a renvoyé {detectedPitch} Hz au lieu de 110 Hz.");
    }

    /// <summary>
    /// TEST AUX LIMITES (ATTÉNUATION) : Quand le son de la guitare s'éteint (fin de note),
    /// le signal ne contient plus assez d'énergie. L'algorithme doit renvoyer -1 pour éviter les fausses notes.
    /// </summary>
    [Fact]
    public void ComputeYinPitch_WithExtremelyWeakSignal_ShouldReturnNegativeOne()
    {
        // Arrange : Signal à 440 Hz atténué à 99.9% (proche du silence numérique)
        float[] audioBuffer = GeneratePureSineWave(440.0, SampleRate, WindowSize);
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            audioBuffer[i] *= 0.001f; 
        }

        var pitchService = new PublicPitchDetectionService();

        // Act
        double detectedPitch = pitchService.ExposeComputeYinPitch(audioBuffer, SampleRate, 0.15);

        // Assert : L'énergie est trop faible, le traitement doit être ignoré.
        Assert.Equal(-1, detectedPitch);
    }

    #region Helpers Mathématiques de traitement du signal

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