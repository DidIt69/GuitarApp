using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using GuitarApp.Core.Models;
using GuitarApp.Infrastructure.Data;

namespace GuitarApp.UnitTests;

public class SQLiteRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SQLiteRepository<GuitarSession> _sessionRepo;
    private readonly SQLiteRepository<DetectedNoteLog> _noteRepo;

    public SQLiteRepositoryTests()
    {
        // Utilisation d'un fichier de base de données temporaire unique pour ce scénario de test
        _dbPath = Path.Combine(Path.GetTempPath(), $"guitar_test_{Guid.NewGuid()}.db3");
        
        // Initialisation des dépôts avec la même base de données
        _sessionRepo = new SQLiteRepository<GuitarSession>(_dbPath);
        _noteRepo = new SQLiteRepository<DetectedNoteLog>(_dbPath);
    }

    /// <summary>
    /// TEST D'INSERTION ET LECTURE : Vérifie qu'une session s'enregistre et génère son ID automatique.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ShouldInsertSessionAndGenerateAutoIncrementId()
    {
        // Arrange
        var session = new GuitarSession { Title = "Session Blues" };

        // Act
        int result = await _sessionRepo.SaveAsync(session);
        var allSessions = await _sessionRepo.GetAllAsync();

        // Assert
        Assert.True(result > 0, "L'opération d'insertion a échoué.");
        Assert.Single(allSessions);
        Assert.Equal("Session Blues", allSessions[0].Title);
        Assert.True(allSessions[0].Id > 0, "L'ID auto-incrémenté n'a pas été généré.");
    }

    /// <summary>
    /// TEST DE FILTRAGE RELATIONNEL : Vérifie que le filtrage par prédicat LINQ (FindAsync)
    /// extrait uniquement les notes rattachées à la bonne session de guitare.
    /// </summary>
    [Fact]
    public async Task FindAsync_WithSessionIdPredicate_ShouldReturnOnlyNotesFromThatSession()
    {
        // Arrange : Création de deux sessions distinctes
        var sessionA = new GuitarSession { Title = "Session Rock" };
        var sessionB = new GuitarSession { Title = "Session Jazz" };
        await _sessionRepo.SaveAsync(sessionA);
        await _sessionRepo.SaveAsync(sessionB);

        // Ajout de notes sur la Session A (Id de sessionA)
        var noteA1 = new DetectedNoteLog { SessionId = sessionA.Id, NoteName = "Mi (2)", Frequency = 82.41 };
        var noteA2 = new DetectedNoteLog { SessionId = sessionA.Id, NoteName = "La (2)", Frequency = 110.00 };
        // Ajout d'une note sur la Session B (Id de sessionB)
        var noteB1 = new DetectedNoteLog { SessionId = sessionB.Id, NoteName = "Mi (4)", Frequency = 329.63 };

        await _noteRepo.SaveAsync(noteA1);
        await _noteRepo.SaveAsync(noteA2);
        await _noteRepo.SaveAsync(noteB1);

        // Act : Extraction exclusive des notes appartenant à la Session A via notre expression LINQ générique
        var notesSessionA = await _noteRepo.FindAsync(note => note.SessionId == sessionA.Id);

        // Assert
        Assert.Equal(2, notesSessionA.Count);
        Assert.Contains(notesSessionA, n => n.NoteName == "Mi (2)");
        Assert.Contains(notesSessionA, n => n.NoteName == "La (2)");
        Assert.DoesNotContain(notesSessionA, n => n.NoteName == "Mi (4)"); // La note de la session Jazz ne doit pas apparaître
    }

    /// <summary>
    /// TEST DE SUPPRESSION : Valide le nettoyage d'une entité.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveSessionFromDatabase()
    {
        // Arrange
        var session = new GuitarSession { Title = "A supprimer" };
        await _sessionRepo.SaveAsync(session);

        // Act
        await _sessionRepo.DeleteAsync(session);
        var allSessions = await _sessionRepo.GetAllAsync();

        // Assert
        Assert.Empty(allSessions);
    }

    /// <summary>
    /// Nettoyage automatique après l'exécution de chaque test pour laisser le système propre.
    /// </summary>
    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch
            {
                // Ignorer les erreurs si le fichier est temporairement verrouillé
            }
        }
    }
}
