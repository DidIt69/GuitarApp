#region Imports
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GuitarApp.Core.Services;
using GuitarApp.Core.Models;
using System.Collections.ObjectModel;
#endregion

namespace GuitarApp.Core.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Services
    private readonly ITelemetryService _telemetryService;
    private readonly IPitchDetectionService _pitchService;
    private readonly ITuningEvaluator _tuningEvaluator;
    private readonly IRepository<GuitarSession> _sessionRepository;
    private readonly IRepository<DetectedNoteLog> _noteRepository;
    #endregion

    #region Attributs
    private GuitarSession? _currentActiveSession;
    private string _lastRegisteredNote = string.Empty;

    // Tampons de vote à majorité historique (Scoring anti-clignotement)
    private readonly Queue<string> _singleNoteHistory = new();
    private readonly Queue<string> _chordHistory = new();
    private const int HistoryBufferSize = 12;
    #endregion

    #region Properties
    [ObservableProperty] private string _detectedNote = "---";
    [ObservableProperty] private double _currentFrequency;
    [ObservableProperty] private double _centsError;
    [ObservableProperty] private double _accuracyScore;
    [ObservableProperty] private string _tuningIndicatorColor = "#808080";
    [ObservableProperty] private string _statusMessage = "Prêt à débuter";
    [ObservableProperty] private string _actionButtonText = "Démarrer la Session";
    [ObservableProperty] private AudioSourceType _selectedSource = AudioSourceType.Microphone;
    [ObservableProperty] private float[] _waveformBuffer = new float[0];
    [ObservableProperty] private string _detectedChordNotes = "---";
    [ObservableProperty] private ObservableCollection<DetectedNoteLog> _liveSessionNotes = new();
    #endregion

    public List<AudioSourceType> AvailableSources => Enum.GetValues(typeof(AudioSourceType)).Cast<AudioSourceType>().ToList();

    #region Constructors
    /// <summary>
    /// 
    /// </summary>
    /// <param name="telemetryService"></param>
    /// <param name="pitchService"></param>
    /// <param name="tuningEvaluator"></param>
    /// <param name="sessionRepository"></param>
    /// <param name="noteRepository"></param>
    public MainViewModel(
        ITelemetryService telemetryService,
        IPitchDetectionService pitchService,
        ITuningEvaluator tuningEvaluator,
        IRepository<GuitarSession> sessionRepository,
        IRepository<DetectedNoteLog> noteRepository)
    {
        _telemetryService = telemetryService;
        _pitchService = pitchService;
        _tuningEvaluator = tuningEvaluator;
        _sessionRepository = sessionRepository;
        _noteRepository = noteRepository;

        Title = "Analyseur de Guitare Expert";
        _pitchService.PitchDetected += OnPitchDetected;
        _pitchService.ChordPitchesDetected += OnChordPitchesDetected;
        _pitchService.WaveformReceived += OnWaveformReceived;
    }
    #endregion

    #region Commands
    [RelayCommand]
    private async Task ToggleListeningAsync()
    {
        if (IsBusy)
        {
            await _pitchService.StopListeningAsync();
            IsBusy = false;
            _currentActiveSession = null;
            ActionButtonText = "Démarrer la Session";
            ResetInterfaceToSilence();
        }
        else
        {
            IsBusy = true;
            ActionButtonText = "Arrêter la Session";
            StatusMessage = "Écoute active...";
            LiveSessionNotes.Clear();
            _lastRegisteredNote = string.Empty;

            _currentActiveSession = new GuitarSession { Title = $"Session du {DateTime.Now:dd/MM à HH:mm}" };
            await _sessionRepository.SaveAsync(_currentActiveSession);
            await _pitchService.StartListeningAsync(SelectedSource);
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// 
    /// </summary>
    /// <param name="frequency"></param>
    private void OnPitchDetected(double frequency)
    {
        string detectedNoteThisCycle = "---";

        if (frequency > 60)
        {
            TuningResult result = _tuningEvaluator.EvaluatePitch(frequency);
            if (result.NoteName != "Hors plage")
            {
                detectedNoteThisCycle = ConvertHzToNote(frequency);
            }
        }

        _singleNoteHistory.Enqueue(detectedNoteThisCycle);
        if (_singleNoteHistory.Count > HistoryBufferSize)
        {
            _singleNoteHistory.Dequeue();
        }

        var majorityNote = _singleNoteHistory.GroupBy(n => n).OrderByDescending(g => g.Count()).First();

        if (majorityNote.Count() >= (HistoryBufferSize * 0.6))
        {
            if (majorityNote.Key == "---")
            {
                if (DetectedChordNotes == "---")
                {
                    ResetInterfaceToSilence();
                }
            }
            else
            {
                DetectedNote = majorityNote.Key;
                if (frequency > 60)
                {
                    TuningResult res = _tuningEvaluator.EvaluatePitch(frequency);
                    CurrentFrequency = Math.Round(frequency, 2);
                    CentsError = res.CentsError;
                    AccuracyScore = res.AccuracyScore;
                    TuningIndicatorColor = res.IsInTune ? "#2B7A4B" : (res.CentsError < 0 ? "#D97706" : "#DC2626");
                    StatusMessage = res.IsInTune ? "Parfaitement juste !" : (res.CentsError < 0 ? "Trop bas" : "Trop haut");

                    if (_currentActiveSession != null && res.NoteName != _lastRegisteredNote)
                    {
                        _lastRegisteredNote = res.NoteName;
                        var noteLog = new DetectedNoteLog
                        {
                            SessionId = _currentActiveSession.Id,
                            NoteName = DetectedNote,
                            Frequency = CurrentFrequency,
                            CentsError = CentsError,
                            AccuracyScore = AccuracyScore,
                            IsInTune = res.IsInTune
                        };
                        LiveSessionNotes.Insert(0, noteLog);
                        Task.Run(async () => await _noteRepository.SaveAsync(noteLog));
                    }
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="frequencies"></param>
    private void OnChordPitchesDetected(List<double> frequencies)
    {
        string detectedChordThisCycle = "---";

        if (DetectedNote != "---" && !DetectedNote.Contains("Majeur") && !DetectedNote.Contains("Mineur") && _singleNoteHistory.Count(n => n != "---") > 5)
        {
            DetectedChordNotes = "---";
            _chordHistory.Clear();
            return;
        }

        if (frequencies != null && frequencies.Any())
        {
            List<string> notesInChord = new();
            foreach (double freq in frequencies)
            {
                string name = ConvertHzToNoteNameOnly(freq);
                if (!notesInChord.Contains(name) && name != "Hors plage")
                {
                    notesInChord.Add(name);
                }
            }

            if (notesInChord.Count >= 2)
            {
                string calculatedChord = ChordRecognizer.IdentifyChord(notesInChord);
                if (calculatedChord != "---" && !calculatedChord.Contains('-'))
                {
                    detectedChordThisCycle = calculatedChord;
                }
            }
        }

        _chordHistory.Enqueue(detectedChordThisCycle);
        if (_chordHistory.Count > HistoryBufferSize)
        {
            _chordHistory.Dequeue();
        }

        var majorityChord = _chordHistory.GroupBy(c => c).OrderByDescending(g => g.Count()).First();

        if (majorityChord.Count() >= (HistoryBufferSize * 0.5))
        {
            DetectedChordNotes = majorityChord.Key;
            if (majorityChord.Key != "---")
            {
                DetectedNote = majorityChord.Key;
                StatusMessage = "Accord Détecté !";
                TuningIndicatorColor = "#2B7A4B";
                CentsError = 0;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rawSamples"></param>
    private void OnWaveformReceived(float[] rawSamples)
    {
        float[] displaySamples = new float[128];
        int step = rawSamples.Length / displaySamples.Length;

        for (int i = 0; i < displaySamples.Length; i++)
        {
            displaySamples[i] = rawSamples[i * step];
        }

        WaveformBuffer = displaySamples;
    }

    /// <summary>
    /// 
    /// </summary>
    private void ResetInterfaceToSilence()
    {
        _singleNoteHistory.Clear();
        _chordHistory.Clear();

        DetectedNote = "---";
        DetectedChordNotes = "---";
        CurrentFrequency = 0;
        CentsError = 0;
        AccuracyScore = 0;

        TuningIndicatorColor = "#808080";
        StatusMessage = "En attente d'un signal...";
        WaveformBuffer = new float[128];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="freq"></param>
    /// <returns></returns>
    private string ConvertHzToNote(double freq)
    {
        if (freq < 40 || freq > 2000)
        {
            return "Hors plage";
        }

        if (freq >= 102.0 && freq <= 108.0)
        {
            freq = 110.0; // Redressement d'octave instrumentale Fender
        }

        string[] notes = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };
        double hertzLa = 440.0;
        double distance = 12.0 * Math.Log(freq / hertzLa) / Math.Log(2.0);
        int noteIndex = (int)Math.Round(distance + 69.0);
        
        return $"{notes[noteIndex % 12]} ({(noteIndex / 12) - 1})";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="freq"></param>
    /// <returns></returns>
    private string ConvertHzToNoteNameOnly(double freq)
    {
        if (freq < 40 || freq > 2000)
        {
            return "Hors plage";
        }

        if (freq >= 102.0 && freq <= 108.0)
        {
            freq = 110.0;
        }

        string[] notes = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };
        int noteIndex = (int)Math.Round(12.0 * Math.Log(freq / 440.0) / Math.Log(2.0) + 69.0);
        
        return notes[noteIndex % 12];
    }
    #endregion
}
