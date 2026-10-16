namespace GuitarApp.Core.Services;

public static class ChordRecognizer
{
    /// <summary>
    /// Liste des accords
    /// </summary>
    private static readonly Dictionary<string, int[]> ChordPatterns = new()
    {
        { "Majeur", new[] { 4, 7 } },
        { "Mineur", new[] { 3, 7 } },
        { "7ème", new[] { 4, 7, 10 } },
        { "Maj7", new[] { 4, 7, 11 } },
        { "m7", new[] { 3, 7, 10 } }
    };

    /// <summary>
    /// Liste des notes
    /// </summary>
    private static readonly string[] NoteNames = { "Do", "Do#", "Ré", "Ré#", "Mi", "Fa", "Fa#", "Sol", "Sol#", "La", "La#", "Si" };

    /// <summary>
    /// Méthode permettant d'identifiant un accord
    /// </summary>
    /// <param name="detectedNotes">Liste des notes detectées</param>
    /// <returns></returns>
    public static string IdentifyChord(List<string> detectedNotes)
    {
        if (detectedNotes == null || detectedNotes.Count < 2)
        {
            return "---";
        }

        var uniqueNotes = detectedNotes.Select(n => n.Trim()).Distinct().ToList();
        if (uniqueNotes.Count < 2)
        {
            return "---";
        }

        foreach (var rootNote in uniqueNotes)
        {
            int rootIndex = Array.IndexOf(NoteNames, rootNote);

            if (rootIndex == -1)
            {
                continue;
            }

            var intervals = new HashSet<int>();
            foreach (var note in uniqueNotes)
            {
                int noteIndex = Array.IndexOf(NoteNames, note);
                if (noteIndex == -1)
                {
                    continue;
                }

                int interval = (noteIndex - rootIndex + 12) % 12;
                intervals.Add(interval);
            }

            foreach (var pattern in ChordPatterns)
            {
                if (pattern.Value.All(req => intervals.Contains(req)))
                {
                    return $"{rootNote} {pattern.Key}";
                }
            }
        }

        return string.Join(" - ", uniqueNotes);
    }
}
