using PanBug.Domain.Model;

namespace PanBug.Domain.Scoring;

public sealed record TaskScore(
    uint TaskNumber,
    int AwardedPoints,
    int MaxPoints,
    bool Reproduced,
    bool CategoryMatched);

public sealed record ScoreResult(int Points, int MaxPoints, IReadOnlyList<TaskScore> Breakdown);

/// <summary>Ocenia raport z sesji i wylicza zdobyte punkty.</summary>
public interface IScoringEngine
{
    ScoreResult Score(Report report);
}
