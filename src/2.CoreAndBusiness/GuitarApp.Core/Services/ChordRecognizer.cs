using System;
using System.Collections.Generic;
using System.Linq;

namespace GuitarApp.Core.Services;

public static class ChordRecognizer
{
    private static readonly Dictionary<string, int[]> ChordPatterns = new()
    {
        { "Majeur", new[] { 4, 7 } },       // Fondamentale + Tierce Majeure (4) + Quinte (7)
        { "Mineur", new[] { 3, 7 } },       // Fondamentale + Tierce Mineure (3) + Quinte (7)
        { "7ème", new[] { 4, 7, 10 } },
        { "Maj7", new[] { 4, 7, 11 } },
        { "m7", new[] { 3, 7, 10 } }
    };

    private static readonly string[] NoteNames = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };

    public static string IdentifyChord(List<string> detectedNotes)
    {
        if (detectedNotes == null || detectedNotes.Count < 2) return "---";

        // Nettoyage strict des doublons de notes (ex: s'il y a deux "Do", on n'en garde qu'un)
        var uniqueNotes = detectedNotes
            .Select(n => n.Trim())
            .Distinct()
            .ToList();

        if (uniqueNotes.Count < 2) return "---";

        // Parcourir chaque note présente pour tester si elle peut être la fondamentale de l'accord
        foreach (var rootNote in uniqueNotes)
        {
            int rootIndex = Array.IndexOf(NoteNames, rootNote);
            if (rootIndex == -1) continue;

            // Calculer les distances en demi-tons de toutes les autres notes par rapport à cette fondamentale
            var intervals = new HashSet<int>();
            foreach (var note in uniqueNotes)
            {
                int noteIndex = Array.IndexOf(NoteNames, note);
                if (noteIndex == -1) continue;

                // Modulo 12 indispensable pour gérer l'écart de demi-tons de manière circulaire (Do à Si)
                int interval = (noteIndex - rootIndex + 12) % 12;
                intervals.Add(interval);
            }

            // Comparaison avec les structures d'intervalles de notre dictionnaire
            foreach (var pattern in ChordPatterns)
            {
                if (pattern.Value.All(req => intervals.Contains(req)))
                {
                    return $"{rootNote} {pattern.Key}"; // Ex: "Do Majeur"
                }
            }
        }

        // Si l'accord est inconnu mais contient plusieurs notes, on affiche la liste brute des notes
        return string.Join(" - ", uniqueNotes);
    }
}
