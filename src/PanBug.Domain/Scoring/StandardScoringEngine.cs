using PanBug.Domain.Defects;
using PanBug.Domain.Model;

namespace PanBug.Domain.Scoring;

public sealed class ScoringOptions
{
    /// <summary>
    /// Gdy true, przyznaje połowę punktów (zaokrągloną w górę) za reprodukcję
    /// defektu bez trafienia poprawnej kategorii. Domyślnie wyłączone: punkty
    /// daje tylko odtworzony defekt zgłoszony z pasującą kategorią.
    /// </summary>
    public bool PartialCreditForReproductionOnly { get; init; }
}

/// <summary>
/// Odtworzona logika punktacji serwera. Zadanie daje pełne punkty, gdy log akcji
/// potwierdza reprodukcję defektu i zgłoszone kategorie zawierają poprawną.
/// </summary>
public sealed class StandardScoringEngine : IScoringEngine
{
    private readonly ScoringOptions _options;

    public StandardScoringEngine(ScoringOptions? options = null) =>
        _options = options ?? new ScoringOptions();

    public ScoreResult Score(Report report)
    {
        var breakdown = new List<TaskScore>();
        int total = 0;

        foreach (var task in report.Tasks)
        {
            if (!DefectCatalog.TryGet(task.Number, out var defect))
                continue;

            int max = defect.PointsValue;
            int awarded = 0;
            bool reproduced = false;
            bool categoryMatched = false;

            if (task.IsCompleted)
            {
                reproduced = defect.IsReproduced(task.Actions);
                categoryMatched = defect.CategoriesMatch(task.AnswerNumbers);

                if (reproduced && categoryMatched)
                    awarded = max;
                else if (reproduced && _options.PartialCreditForReproductionOnly)
                    awarded = (max + 1) / 2;
            }

            total += awarded;
            breakdown.Add(new TaskScore(task.Number, awarded, max, reproduced, categoryMatched));
        }

        return new ScoreResult(total, DefectCatalog.MaxPoints, breakdown);
    }
}
