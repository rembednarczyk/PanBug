using PanBug.Domain.Common;
using PanBug.Domain.Model;

namespace PanBug.Domain.Scoring;

/// <summary>
/// Wynik gry do wklejenia w komunikatorze, w stylu Wordle: punkty, liczba zadań, czas
/// i kwadrat na każde zadanie w kolejności gry. Nie zawiera nazw aplikacji, defektów
/// ani kategorii, więc niczego nie zdradza osobie, która gry jeszcze nie przeszła.
/// </summary>
public static class ShareText
{
    public const string Solved = "🟩";
    public const string WrongCategory = "🟨";
    public const string NotReproduced = "🟥";
    public const string NotReported = "⬜";

    /// <summary>Kwadratów w jednym wierszu: 24 zadania dają trzy równe rzędy.</summary>
    public const int PerRow = 8;

    public static string Build(ScoreResult score, Report report, string url)
    {
        var completed = report.Tasks.Where(t => t.IsCompleted).Select(t => t.Number).ToHashSet();
        var squares = score.Breakdown
            .OrderBy(t => t.TaskNumber)
            .Select(t => Square(t, completed.Contains(t.TaskNumber)))
            .ToList();

        var lines = new List<string>
        {
            $"PanBug: {score.Points}/{score.MaxPoints} pkt, " +
            $"zadania {report.NumberOfCompletedTasks}/{report.Tasks.Count}, " +
            $"czas {Utils.FormatDuration(report.Duration)}",
        };
        for (var i = 0; i < squares.Count; i += PerRow)
            lines.Add(string.Concat(squares.Skip(i).Take(PerRow)));
        if (!string.IsNullOrWhiteSpace(url))
            lines.Add(url);
        return string.Join("\n", lines);
    }

    private static string Square(TaskScore task, bool completed) =>
        !completed ? NotReported
        : task.MaxPoints > 0 && task.AwardedPoints == task.MaxPoints ? Solved
        : task.Reproduced ? WrongCategory
        : NotReproduced;
}
