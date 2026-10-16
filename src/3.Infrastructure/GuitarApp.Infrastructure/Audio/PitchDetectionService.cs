using GuitarApp.Core.Services;
using GuitarApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NAudio.Wave;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace GuitarApp.Infrastructure.Audio;

public class PitchDetectionService : IPitchDetectionService
{
    public event Action<double>? PitchDetected;
    public event Action<List<double>>? ChordPitchesDetected;
    public event Action<float[]>? WaveformReceived;

    private IWaveIn? _audioCapture;
    private const int WindowSize = 4096;
    private const int HpsDownsamples = 3;
    private const double YinThreshold = 0.40; // Seuil optimisé cordes graves Fender
    private int _actualSampleRate = 44100;

    public Task StartListeningAsync(AudioSourceType sourceType)
    {
        if (sourceType == AudioSourceType.InternalDeviceLoopback)
        {
            var loopbackCapture = new WasapiLoopbackCapture();
            _audioCapture = loopbackCapture;
            _actualSampleRate = _audioCapture.WaveFormat.SampleRate;

            _audioCapture.DataAvailable += (s, e) =>
            {
                float[] buffer = new float[WindowSize];
                int bytesPerSample = _audioCapture.WaveFormat.BitsPerSample / 8;
                if (bytesPerSample == 0) bytesPerSample = 4;

                double sum = 0;
                for (int i = 0; i < WindowSize && (i * bytesPerSample) < e.BytesRecorded; i++)
                {
                    if (bytesPerSample == 4)
                        buffer[i] = BitConverter.ToSingle(e.Buffer, i * bytesPerSample);
                    else if (bytesPerSample == 2)
                        buffer[i] = BitConverter.ToInt16(e.Buffer, i * bytesPerSample) / 32768f;

                    buffer[i] *= 2.0f; // Gain logiciel stable
                    if (buffer[i] > 1.0f) buffer[i] = 1.0f;
                    if (buffer[i] < -1.0f) buffer[i] = -1.0f;
                    sum += buffer[i] * buffer[i];
                }

                double rms = Math.Sqrt(sum / WindowSize);

                // Noise Gate permissif pour capter toute la durée du son Fender (sustain)
                if (rms < 0.003)
                {
                    WaveformReceived?.Invoke(new float[WindowSize]);
                    PitchDetected?.Invoke(0);
                    ChordPitchesDetected?.Invoke(new List<double>());
                    return;
                }

                // Affichage immédiat des vagues sur l'oscilloscope vert
                WaveformReceived?.Invoke(buffer);

                // 1. Calcul spectral via la FFT (Très robuste sur les basses)
                List<double> chordPitches = DetectChordPitches(buffer, _actualSampleRate);

                // 2. Calcul temporel via YIN (Très précis sur les aigus)
                double singlePitch = ComputeYinPitch(buffer, _actualSampleRate, YinThreshold);

                // 3. 💡 ALGORITHME D'AIGUILLAGE EXPERT
                // Si la FFT détecte qu'un La grave (~110Hz) ou un Mi grave (~82Hz) est joué,
                // on court-circuite YIN qui se fait piéger par la phase, et on utilise la FFT.
                if (chordPitches != null && chordPitches.Any())
                {
                    double lowestFftPitch = chordPitches.Min();

                    // Cibles des cordes graves : Mi grave (75-90Hz) ou La (100-120Hz)
                    if ((lowestFftPitch >= 75 && lowestFftPitch <= 90) || (lowestFftPitch >= 100 && lowestFftPitch <= 120))
                    {
                        singlePitch = lowestFftPitch;
                    }
                }

                // 4. Émission finale vers le ViewModel
                if (singlePitch > 60 && singlePitch < 1200)
                {
                    PitchDetected?.Invoke(singlePitch);
                }
                else
                {
                    PitchDetected?.Invoke(0);
                }

                if (chordPitches != null && chordPitches.Any())
                {
                    ChordPitchesDetected?.Invoke(chordPitches);
                }
                else
                {
                    ChordPitchesDetected?.Invoke(new List<double>());
                }
            };

            _audioCapture.StartRecording();
        }
        else
        {
            // (Conservez votre code de capture microphone standard intact...)
            var micCapture = new WaveInEvent();
            micCapture.WaveFormat = new WaveFormat(44100, 16, 1);
            _audioCapture = micCapture;
            _actualSampleRate = 44100;
            _audioCapture.DataAvailable += (s, e) => { /* ... code du micro ... */ };
            _audioCapture.StartRecording();
        }
        return Task.CompletedTask;
    }

    public Task StopListeningAsync()
    {
        if (_audioCapture != null) { _audioCapture.StopRecording(); _audioCapture.Dispose(); _audioCapture = null; }
        return Task.CompletedTask;
    }

    // 💡 AJUSTEMENT DE LA FENÊTRE TEMPORELLE YIN POUR LES GRAVES
    private double ComputeYinPitch(float[] buffer, int sampleRate, double threshold)
    {
        // 💡 Pour une fenêtre de 4096, on fixe tauMax à la moitié pour avoir 2048 points de recherche,
        // ce qui permet d'aller chercher des périodes très longues jusqu'à 23 Hz.
        int tauMax = buffer.Length / 2;
        double[] yinBuffer = new double[tauMax];

        // Étape 1 : Calcul de la fonction de différence cumulative
        for (int tau = 1; tau < tauMax; tau++)
        {
            // Fenêtre d'intégration fixe de taille tauMax pour stabiliser les cordes graves
            for (int i = 0; i < tauMax; i++)
            {
                double delta = buffer[i] - buffer[i + tau];
                yinBuffer[tau] += delta * delta;
            }
        }

        // Étape 2 : Normalisation cumulative unifiée
        double runningSum = 0;
        yinBuffer[0] = 1.0;
        for (int tau = 1; tau < tauMax; tau++)
        {
            runningSum += yinBuffer[tau];
            if (runningSum != 0) yinBuffer[tau] *= tau / runningSum;
            else yinBuffer[tau] = 1.0;
        }

        // Étape 3 : Recherche stricte du fond du premier creux local sous le seuil
        int period = -1;
        for (int tau = 2; tau < tauMax - 1; tau++)
        {
            if (yinBuffer[tau] < threshold)
            {
                // On glisse jusqu'au point le plus bas du creux (le minimum local)
                while (tau + 1 < tauMax && yinBuffer[tau + 1] < yinBuffer[tau])
                {
                    tau++;
                }
                period = tau;
                break;
            }
        }

        // Étape 4 : Interpolation parabolique de haute précision pour affiner le résultat
        if (period > 0 && period < tauMax - 1)
        {
            double s0 = yinBuffer[period - 1];
            double s1 = yinBuffer[period];
            double s2 = yinBuffer[period + 1];

            double denominator = 2.0 * (s2 - 2.0 * s1 + s0);
            if (Math.Abs(denominator) > 0.000001)
            {
                double deltaPeriod = (s0 - s2) / denominator;
                double refinedPeriod = period + deltaPeriod;
                return (double)sampleRate / refinedPeriod;
            }
        }

        return period > 0 ? (double)sampleRate / period : -1;
    }


    //private double ComputeYinPitch(float[] buffer, int sampleRate, double threshold)
    //{
    //    int tauMax = buffer.Length / 2;
    //    double[] yinBuffer = new double[tauMax];
    //    for (int tau = 1; tau < tauMax; tau++)
    //    {
    //        for (int i = 0; i < tauMax; i++)
    //        {
    //            double delta = buffer[i] - buffer[i + tau];
    //            yinBuffer[tau] += delta * delta;
    //        }
    //    }
    //    double runningSum = 0; yinBuffer[0] = 1;
    //    for (int tau = 1; tau < tauMax; tau++)
    //    {
    //        runningSum += yinBuffer[tau];
    //        if (runningSum != 0) yinBuffer[tau] *= tau / runningSum;
    //        else yinBuffer[tau] = 1;
    //    }
    //    int period = -1;
    //    for (int tau = 1; tau < tauMax; tau++)
    //    {
    //        if (yinBuffer[tau] < threshold)
    //        {
    //            while (tau + 1 < tauMax && yinBuffer[tau + 1] < yinBuffer[tau]) tau++;
    //            period = tau; break;
    //        }
    //    }
    //    return period > 0 ? (double)sampleRate / period : -1;
    //}

    private List<double> DetectChordPitches(float[] audioBuffer, int sampleRate)
    {
        List<double> detectedPitches = new(); int n = audioBuffer.Length;
        Complex[] fftBuffer = new Complex[n];
        for (int i = 0; i < n; i++)
        {
            double window = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (n - 1)));
            fftBuffer[i] = new Complex(audioBuffer[i] * window, 0);
        }
        Fourier.Forward(fftBuffer, FourierOptions.Default);
        int spectrumLength = n / 2; double[] magnitudeSpectrum = new double[spectrumLength];
        for (int i = 0; i < spectrumLength; i++) magnitudeSpectrum[i] = fftBuffer[i].Magnitude;
        double maxMagnitude = magnitudeSpectrum.Max(); if (maxMagnitude == 0) maxMagnitude = 1;

        double resolutionHz = (double)sampleRate / n;

        // Étape 1 : On fait un premier passage rapide pour compter combien de pics émergent au-dessus de 30%
        int majorPeaksCount = 0;
        for (int i = 4; i < spectrumLength - 4; i++)
        {
            if (magnitudeSpectrum[i] > magnitudeSpectrum[i - 1] && magnitudeSpectrum[i] > magnitudeSpectrum[i + 1])
            {
                if ((magnitudeSpectrum[i] / maxMagnitude) > 0.30) majorPeaksCount++;
            }
        }

        // Étape 2 : Recherche et filtrage adaptatif des pics
        for (int i = 4; i < spectrumLength - 4; i++)
        {
            if (magnitudeSpectrum[i] > magnitudeSpectrum[i - 1] && magnitudeSpectrum[i] > magnitudeSpectrum[i + 1] &&
                magnitudeSpectrum[i] > magnitudeSpectrum[i - 2] && magnitudeSpectrum[i] > magnitudeSpectrum[i + 2])
            {
                double freq = i * resolutionHz;

                // 💡 LE FILTRE D'ARBITRAGE :
                // Si on détecte un accord (plusieurs pics majeurs), on force un seuil strict à 30% (0.30)
                // pour balayer le clignotement et les harmoniques parasites.
                // On n'autorise le seuil bas à 10% (0.10) QUE si une seule corde grave isolée est jouée (comme votre La).
                double adaptiveThreshold = (majorPeaksCount <= 1 && freq < 130.0) ? 0.10 : 0.30;

                if (freq >= 65 && freq <= 900 && (magnitudeSpectrum[i] / maxMagnitude) > adaptiveThreshold)
                {
                    if (!detectedPitches.Any(f => Math.Abs(f - freq) < 12.0))
                    {
                        detectedPitches.Add(freq);
                    }
                }
            }
        }

        return detectedPitches;
    }
}
