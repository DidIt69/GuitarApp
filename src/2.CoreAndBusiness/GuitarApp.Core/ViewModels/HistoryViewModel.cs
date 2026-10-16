#region Imports
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GuitarApp.Core.Models;
using GuitarApp.Core.Services;
#endregion

namespace GuitarApp.Core.ViewModels;

public partial class HistoryViewModel : ViewModelBase
{
    #region Services
    private readonly IRepository<GuitarSession> _sessionRepository;
    private readonly IRepository<DetectedNoteLog> _noteRepository;
    private readonly ITelemetryService _telemetryService;
    #endregion

    #region Properties
    [ObservableProperty] private ObservableCollection<GuitarSessionDisplay> _sessions = new();
    #endregion

    #region Constructors
    public HistoryViewModel(
        IRepository<GuitarSession> sessionRepository,
        IRepository<DetectedNoteLog> noteRepository,
        ITelemetryService telemetryService)
    {
        _sessionRepository = sessionRepository;
        _noteRepository = noteRepository;
        _telemetryService = telemetryService;
        Title = "Historique & Performances";
    }
    #endregion

    #region Override Methods
    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameter"></param>
    /// <returns></returns>
    public override async Task InitializeAsync(object? parameter)
    {
        IsBusy = true;
        try
        {
            var rawSessions = await _sessionRepository.GetAllAsync();
            var displayList = new List<GuitarSessionDisplay>();

            foreach (var session in rawSessions)
            {
                var notes = await _noteRepository.FindAsync(n => n.SessionId == session.Id);
                double avgAccuracy = notes.Any() ? notes.Average(n => n.AccuracyScore) : 0;
                string color = avgAccuracy >= 85 ? "#2B7A4B" : (avgAccuracy >= 50 ? "#D97706" : "#DC2626");

                displayList.Add(new GuitarSessionDisplay
                {
                    Id = session.Id,
                    DateSession = session.DateSession,
                    Title = session.Title,
                    AverageAccuracy = Math.Round(avgAccuracy, 0),
                    TotalNotesCount = notes.Count,
                    PerformanceColor = color
                });
            }
            Sessions = new ObservableCollection<GuitarSessionDisplay>(displayList.OrderByDescending(s => s.DateSession));
        }
        catch (Exception ex)
        {
            _telemetryService.LogError("Erreur historique", ex);
        }
        finally 
        { 
            IsBusy = false; 
        }
    }
    #endregion

    #region Commands
    [RelayCommand]
    private async Task DeleteSessionAsync(GuitarSessionDisplay sessionDisplay)
    {
        if (sessionDisplay == null) return;
        var raw = await _sessionRepository.GetAllAsync();
        var target = raw.FirstOrDefault(s => s.Id == sessionDisplay.Id);
        if (target != null)
        {
            await _sessionRepository.DeleteAsync(target);
            Sessions.Remove(sessionDisplay);
        }
    }
    #endregion
}
