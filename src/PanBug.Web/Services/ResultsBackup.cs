using System.Text.Json;

namespace PanBug.Web.Services;

/// <summary>Plik kopii wyników. <see cref="Format"/> i <see cref="Version"/> rozpoznają plik przy imporcie.</summary>
public sealed record ResultsBackupFile(
    string Format,
    int Version,
    DateTime ExportedAtUtc,
    GameRecord? Best,
    IReadOnlyList<GameRecord> History);

/// <summary>Wyniki po scaleniu z importem i liczba gier, których wcześniej nie było w historii.</summary>
public sealed record MergedResults(GameRecord? Best, IReadOnlyList<GameRecord> History, int Added);

/// <summary>
/// Eksport i import wyników do pliku JSON. Wyniki żyją tylko w localStorage jednej przeglądarki,
/// więc plik to jedyny sposób, żeby przenieść je na inne urządzenie albo zachować po
/// wyczyszczeniu danych strony. Import scala, nie nadpisuje: gry z pliku dochodzą do historii.
/// </summary>
public static class ResultsBackup
{
    public const string Format = "panbug-results";
    public const int Version = 1;

    /// <summary>Największy plik, jaki przyjmujemy. Pełna kopia ma kilka kB.</summary>
    public const long MaxFileBytes = 256 * 1024;

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static string Serialize(GameRecord? best, IReadOnlyList<GameRecord> history, DateTime nowUtc) =>
        JsonSerializer.Serialize(new ResultsBackupFile(Format, Version, nowUtc, best, history), WriteOptions);

    public static string FileName(DateTime nowLocal) => $"panbug-wyniki-{nowLocal:yyyy-MM-dd}.json";

    /// <summary>
    /// Odczytuje plik kopii. Zwraca null, gdy to nie jest kopia PanBuga albo pochodzi z nowszej
    /// wersji. Uszkodzone pojedyncze wpisy są pomijane, tak jak przy odczycie z localStorage.
    /// </summary>
    public static ResultsBackupFile? Parse(string json)
    {
        ResultsBackupFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ResultsBackupFile>(json);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or ArgumentException)
        {
            return null;
        }

        if (file is null || file.Format != Format || file.Version is < 1 or > Version)
            return null;

        var history = (file.History ?? Array.Empty<GameRecord>())
            .OfType<GameRecord>()
            .Where(g => g.IsPlausible)
            .ToList();
        var best = file.Best is { IsPlausible: true } ? file.Best : null;
        return file with { Best = best, History = history };
    }

    /// <summary>
    /// Scala historię z importem: bez duplikatów (ta sama gra zapisana dwa razy to jeden rekord),
    /// od najnowszej, najwyżej <paramref name="limit"/> wpisów. Najlepszy wynik to lepszy z obu
    /// stron i z całej scalonej historii, także z gier, które nie zmieściły się w limicie.
    /// </summary>
    public static MergedResults Merge(
        GameRecord? currentBest, IReadOnlyList<GameRecord> currentHistory, ResultsBackupFile imported, int limit)
    {
        var known = currentHistory.ToHashSet();
        var all = currentHistory.Concat(imported.History).Distinct().ToList();
        int added = imported.History.Distinct().Count(g => !known.Contains(g));

        GameRecord? best = null;
        foreach (var candidate in all.Append(currentBest).Append(imported.Best))
            if (candidate is not null && candidate.IsBetterThan(best))
                best = candidate;

        var history = all.OrderByDescending(g => g.FinishedAtUtc).Take(limit).ToList();
        return new MergedResults(best, history, added);
    }
}
