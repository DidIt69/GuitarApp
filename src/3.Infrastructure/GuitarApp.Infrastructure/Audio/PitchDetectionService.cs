#region Imports
using GuitarApp.Core.Services;
using GuitarApp.Core.Models;
using NAudio.Wave;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
#endregion
namespace GuitarApp.Infrastructure.Audio;

public class PitchDetectionService : IPitchDetectionService
{
    #region Events
    public event Action<double>? PitchDetected;
    public event Action<List<double>>? ChordPitchesDetected;
    public event Action<float[]>? WaveformReceived;
    #endregion

    #region Attributs
    private IWaveIn? _audioCapture;
    private const int WindowSize = 4096;
    private const int HpsDownsamples = 3;
    private const double YinThreshold = 0.40;
    private int _actualSampleRate = 44100;
    #endregion

    #region Implement IPitchDetectionService
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
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
                    if (bytesPerSample == 4) buffer[i] = BitConverter.ToSingle(e.Buffer, i * bytesPerSample);
                    else if (bytesPerSample == 2) buffer[i] = BitConverter.ToInt16(e.Buffer, i * bytesPerSample) / 32768f;

                    buffer[i] *= 2.0f;
                    if (buffer[i] > 1.0f) buffer[i] = 1.0f;
                    if (buffer[i] < -1.0f) buffer[i] = -1.0f;
                    sum += buffer[i] * buffer[i];
                }

                double rms = Math.Sqrt(sum / WindowSize);
                if (rms < 0.003) // Noise gate anti-souffle permissif
                {
                    WaveformReceived?.Invoke(new float[WindowSize]);
                    PitchDetected?.Invoke(0);
                    ChordPitchesDetected?.Invoke(new List<double>());
                    return;
                }

                WaveformReceived?.Invoke(buffer);

                List<double> chordPitches = DetectChordPitches(buffer, _actualSampleRate);
                double singlePitch = ComputeYinPitch(buffer, _actualSampleRate, YinThreshold);

                // Aiguillage hybride FFT de sécurité pour capturer le La et le Mi grave de Fender
                if (chordPitches != null && chordPitches.Any())
                {
                    double lowestFft = chordPitches.Min();
                    if ((lowestFft >= 75 && lowestFft <= 92) || (lowestFft >= 100 && lowestFft <= 122))
                    {
                        singlePitch = lowestFft;
                    }
                }

                if (singlePitch > 60 && singlePitch < 1200) PitchDetected?.Invoke(singlePitch);
                else PitchDetected?.Invoke(0);

                if (chordPitches != null && chordPitches.Any()) ChordPitchesDetected?.Invoke(chordPitches);
                else ChordPitchesDetected?.Invoke(new List<double>());
            };
            _audioCapture.StartRecording();
        }
        else
        {
            var micCapture = new WaveInEvent();
            micCapture.WaveFormat = new WaveFormat(44100, 16, 1);
            _audioCapture = micCapture;
            _actualSampleRate = 44100;

            _audioCapture.DataAvailable += (s, e) =>
            {
                float[] buffer = new float[WindowSize];
                for (int i = 0; i < WindowSize && (i * 2) < e.BytesRecorded; i++)
                    buffer[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;

                WaveformReceived?.Invoke(buffer);
                double singlePitch = ComputeYinPitch(buffer, _actualSampleRate, YinThreshold);
                if (singlePitch > 60 && singlePitch < 1200) PitchDetected?.Invoke(singlePitch);

                List<double> chordPitches = DetectChordPitches(buffer, _actualSampleRate);
                if (chordPitches != null && chordPitches.Any()) ChordPitchesDetected?.Invoke(chordPitches);
            };
            _audioCapture.StartRecording();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Task StopListeningAsync()
    {
        if (_audioCapture != null) { _audioCapture.StopRecording(); _audioCapture.Dispose(); _audioCapture = null; }
        return Task.CompletedTask;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="sampleRate"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    private double ComputeYinPitch(float[] buffer, int sampleRate, double threshold)
    {
        int tauMax = buffer.Length / 2;
        double[] yinBuffer = new double[tauMax];

        for (int tau = 1; tau < tauMax; tau++)
        {
            for (int i = 0; i < tauMax; i++)
            {
                double delta = buffer[i] - buffer[i + tau];
                yinBuffer[tau] += delta * delta;
            }
        }

        double runningSum = 0;
        yinBuffer[0] = 1.0;

        for (int tau = 1; tau < tauMax; tau++)
        {
            runningSum += yinBuffer[tau];
            if (runningSum != 0) yinBuffer[tau] *= tau / runningSum;
            else yinBuffer[tau] = 1.0;
        }

        int period = -1;
        for (int tau = 2; tau < tauMax - 1; tau++)
        {
            if (yinBuffer[tau] < threshold)
            {
                while (tau + 1 < tauMax && yinBuffer[tau + 1] < yinBuffer[tau]) tau++;
                period = tau; break;
            }
        }

        if (period > 0 && period < tauMax - 1)
        {
            double s0 = yinBuffer[period - 1], s1 = yinBuffer[period], s2 = yinBuffer[period + 1];
            double denom = 2.0 * (s2 - 2.0 * s1 + s0);
            if (Math.Abs(denom) > 0.000001)
            {
                double refinedPeriod = period + ((s0 - s2) / denom);
                return (double)sampleRate / refinedPeriod;
            }
        }
        return period > 0 ? (double)sampleRate / period : -1;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="audioBuffer"></param>
    /// <param name="sampleRate"></param>
    /// <returns></returns>
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

        int majorPeaksCount = 0;
        for (int i = 4; i < spectrumLength - 4; i++)
        {
            if (magnitudeSpectrum[i] > magnitudeSpectrum[i - 1] && magnitudeSpectrum[i] > magnitudeSpectrum[i + 1])
                if ((magnitudeSpectrum[i] / maxMagnitude) > 0.30) majorPeaksCount++;
        }

        for (int i = 4; i < spectrumLength - 4; i++)
        {
            if (magnitudeSpectrum[i] > magnitudeSpectrum[i - 1] && magnitudeSpectrum[i] > magnitudeSpectrum[i + 1] &&
                magnitudeSpectrum[i] > magnitudeSpectrum[i - 2] && magnitudeSpectrum[i] > magnitudeSpectrum[i + 2])
            {
                double freq = i * resolutionHz;
                double adaptiveThreshold = (majorPeaksCount <= 1 && freq < 130.0) ? 0.10 : 0.30;

                if (freq >= 65 && freq <= 900 && (magnitudeSpectrum[i] / maxMagnitude) > adaptiveThreshold)
                {
                    if (!detectedPitches.Any(f => Math.Abs(f - freq) < 12.0)) detectedPitches.Add(freq);
                }
            }
        }
        return detectedPitches;
    }
    #endregion
}
