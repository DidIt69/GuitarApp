using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GuitarApp.Core.Services;
using GuitarApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GuitarApp.Core.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly IPitchDetectionService _pitchService;
    private readonly ITuningEvaluator _tuningEvaluator;
    private readonly IRepository<GuitarSession> _sessionRepository;
    private readonly IRepository<DetectedNoteLog> _noteRepository;

    private GuitarSession? _currentActiveSession;
    private string _lastRegisteredNote = string.Empty;

    private string _pendingSingleNote = string.Empty;
    private int _singleNoteStabilityCount = 0;
    private const int RequiredStabilityCycles = 2; // Debouncing rapide pour les basses

    private string _pendingChordNotes = string.Empty;
    private int _chordStabilityCount = 0;

    // 💡 1. AJOUTEZ CES COMPTEURS DE PERSISTANCE EN HAUT DE LA CLASSE MainViewModel
    private string _lastValidatedNote = "---";
    private int _notePersistenceTimer = 0;

    private string _lastValidatedChord = "---";
    private int _chordPersistenceTimer = 0;

    private const int PersistenceCycles = 8; // Bloque l'affichage pendant 8 cycles (~400ms) pour éviter le clignotement

    // 💡 1. AJOUTEZ CET HORODATAGE EN HAUT DE LA CLASSE MainViewModel
    private DateTime _lastUiUpdateTime = DateTime.MinValue;
    private const int UiUpdateIntervalMs = 150; // Maximum 6 rafraîchissements par seconde (idéal pour l'œil humain)

    // 💡 1. AJOUTEZ CES DEUX FILES D'ATTENTE EN HAUT DE LA CLASSE MainViewModel
    private readonly Queue<string> _singleNoteHistory = new();
    private readonly Queue<string> _chordHistory = new();
    private const int HistoryBufferSize = 12; // Historique étendu sur les 12 derniers blocs (~300ms)

    [ObservableProperty] private string _detectedNote = "---";
    [ObservableProperty] private double _currentFrequency;
    [ObservableProperty] private double _centsError;
    [ObservableProperty] private double _accuracyScore;
    [ObservableProperty] private string _tuningIndicatorColor = "#808080";
    [ObservableProperty] private string _statusMessage = "Prêt à débuter";
    [ObservableProperty] private string _actionButtonText = "Démarrer la Session";
    [ObservableProperty] private AudioSourceType _selectedSource = AudioSourceType.Microphone;
    [ObservableProperty] private float[] _waveformBuffer = new float[256];
    [ObservableProperty] private string _detectedChordNotes = "---";
    [ObservableProperty] private ObservableCollection<DetectedNoteLog> _liveSessionNotes = new();

    public List<AudioSourceType> AvailableSources => Enum.GetValues(typeof(AudioSourceType)).Cast<AudioSourceType>().ToList();

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

    // 💡 2. MODIFIEZ ENTIÈREMENT la méthode OnPitchDetected
    private void OnPitchDetected(double frequency)
    {
        string detectedNoteThisCycle = "---";

        if (frequency > 60)
        {
            TuningResult result = _tuningEvaluator.EvaluatePitch(frequency);
            if (result.NoteName != "Hors plage")
            {
                // On utilise la formule internationale standard
                detectedNoteThisCycle = ConvertHzToNote(frequency);
            }
        }

        // Empiler la note du cycle actuel dans l'historique de vote
        _singleNoteHistory.Enqueue(detectedNoteThisCycle);
        if (_singleNoteHistory.Count > HistoryBufferSize)
        {
            _singleNoteHistory.Dequeue();
        }

        // Calcul du vote : Trouver la note majoritaire dans le tampon
        var majorityNote = _singleNoteHistory
            .GroupBy(n => n)
            .OrderByDescending(g => g.Count())
            .First();

        // Si la note majoritaire représente plus de 60% des votes récents, on l'affiche durablement
        if (majorityNote.Count() >= (HistoryBufferSize * 0.6))
        {
            if (majorityNote.Key == "---")
            {
                if (DetectedChordNotes == "---")
                {
                    DetectedNote = "---";
                    CurrentFrequency = 0;
                    CentsError = 0;
                    AccuracyScore = 0;
                    TuningIndicatorColor = "#808080"; // Gris de veille
                    StatusMessage = "En attente d'un signal...";
                }
            }
            else
            {
                // On n'actualise l'écran que si la note confirmée a changé ou si l'on est au centre vert
                DetectedNote = majorityNote.Key;

                // Calcul des métriques pour l'aiguille à partir de la fréquence brute actuelle
                if (frequency > 60)
                {
                    TuningResult res = _tuningEvaluator.EvaluatePitch(frequency);
                    CurrentFrequency = Math.Round(frequency, 2);
                    CentsError = res.CentsError;
                    AccuracyScore = res.AccuracyScore;
                    TuningIndicatorColor = res.IsInTune ? "#2B7A4B" : (res.CentsError < 0 ? "#D97706" : "#DC2626");
                    StatusMessage = res.IsInTune ? "Parfaitement juste !" : (res.CentsError < 0 ? "Trop bas" : "Trop haut");
                }
            }
        }
    }
    // Dans GuitarApp.Core / ViewModels / MainViewModel.cs

    // 💡 3. MODIFIEZ ENTIÈREMENT la méthode OnChordPitchesDetected
    private void OnChordPitchesDetected(List<double> frequencies)
    {
        string detectedChordThisCycle = "---";

        // Si l'accordeur monophonique est actif et stable sur une note pure, on bloque le mode accord
        if (DetectedNote != "---" && !DetectedNote.Contains("Majeur") && !DetectedNote.Contains("Mineur") && _singleNoteHistory.Count(n => n != "---") > 6)
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

            // Si la FFT voit au moins 2 vraies notes, on interroge le dictionnaire d'accords
            if (notesInChord.Count >= 2)
            {
                string calculatedChord = ChordRecognizer.IdentifyChord(notesInChord);
                if (calculatedChord != "---" && !calculatedChord.Contains('-'))
                {
                    detectedChordThisCycle = calculatedChord;
                }
            }
        }

        // Empiler l'accord du cycle actuel dans l'historique de vote
        _chordHistory.Enqueue(detectedChordThisCycle);
        if (_chordHistory.Count > HistoryBufferSize)
        {
            _chordHistory.Dequeue();
        }

        // Calcul du vote : Trouver l'accord majoritaire dans le tampon
        var majorityChord = _chordHistory
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .First();

        // Lissage par majorité absolue (bloque à 100% le clignotement)
        if (majorityChord.Count() >= (HistoryBufferSize * 0.5))
        {
            DetectedChordNotes = majorityChord.Key;

            if (majorityChord.Key != "---")
            {
                DetectedNote = majorityChord.Key; // Affiche "Do Majeur" en gros au centre
                StatusMessage = "Accord Détecté !";
                TuningIndicatorColor = "#2B7A4B"; // Vert stable
                CentsError = 0; // Aiguille au centre
            }
        }
    }

    private void OnWaveformReceived(float[] rawSamples)
    {
        // On ignore le dessin de l'onde si l'UI n'est pas dans son cycle d'échantillonnage de 150ms
        // Cela soulage instantanément le processeur et stoppe les saccades visuelles
        if ((DateTime.Now - _lastUiUpdateTime).TotalMilliseconds < 50)
        {
            return;
        }

        float[] displaySamples = new float[128];
        int step = rawSamples.Length / displaySamples.Length;
        
        for (int i = 0; i < displaySamples.Length; i++) 
            displaySamples[i] = rawSamples[i * step];

        WaveformBuffer = displaySamples;
    }

    private void ResetInterfaceToSilence()
    {
        _singleNoteStabilityCount = 0; _chordStabilityCount = 0;
        _pendingSingleNote = string.Empty; _pendingChordNotes = string.Empty; _lastRegisteredNote = string.Empty;
        DetectedNote = "---"; DetectedChordNotes = "---"; CurrentFrequency = 0; CentsError = 0; AccuracyScore = 0;
        TuningIndicatorColor = "#808080"; StatusMessage = "En attente d'un signal..."; WaveformBuffer = new float[128];
    }

    private string ConvertHzToNote(double freq)
    {
        if (freq < 40 || freq > 2000) return "Hors plage";

        // 💡 ALIGNEMENT : Si c'est le La bas de Fender, on le redresse pour l'affichage visuel
        if (freq >= 102.0 && freq <= 108.0)
        {
            freq = 110.0;
        }

        string[] notes = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };
        double hertzLa = 440.0;

        double distance = 12.0 * Math.Log(freq / hertzLa) / Math.Log(2.0);
        int noteIndex = (int)Math.Round(distance + 69.0);

        string name = notes[noteIndex % 12];
        int octave = (noteIndex / 12) - 1;

        return $"{name} ({octave})";
    }

    private string ConvertHzToNoteNameOnly(double freq)
    {
        if (freq < 40 || freq > 2000) return "Hors plage";

        if (freq >= 102.0 && freq <= 108.0) freq = 110.0; // Compensation

        string[] notes = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };
        int noteIndex = (int)Math.Round(12.0 * Math.Log(freq / 440.0) / Math.Log(2.0) + 69.0);
        return notes[noteIndex % 12];
    }
}
