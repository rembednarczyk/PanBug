using PanBug.Domain.Model;
using PanBug.Domain.Scoring;
using PanBug.Domain.Session;
using PanBug.Domain.Tasks;

namespace PanBug.Web.Services;

/// <summary>
/// Stan bieżącej gry w przeglądarce: sesja 24 zadań, punktacja liczona lokalnie
/// i wynik ostatniej gry. Najlepszy wynik i historia trafiają do IResultStore.
/// </summary>
public sealed class GameSession
{
    private readonly IScoringEngine _scoring;
    private readonly IResultStore _results;

    public GameSession(IScoringEngine scoring, IResultStore results)
    {
        _scoring = scoring;
        _results = results;
    }

    public TaskManager? Manager { get; private set; }
    public Report? LastReport { get; private set; }
    public ScoreResult? LastScore { get; private set; }
    public GameRecord? Best { get; private set; }
    public bool IsNewRecord { get; private set; }

    public bool HasActiveGame => Manager is { IsCompleted: false };
    public bool HasResult => LastScore is not null;

    public TaskManager StartGame()
    {
        LastReport = null;
        LastScore = null;
        IsNewRecord = false;
        Manager = new TaskManager(TaskCatalog.CreateAll());
        Manager.Run();
        return Manager;
    }

    /// <summary>Kończy grę, liczy punkty i zapisuje wynik w historii.</summary>
    public async Task<ScoreResult> FinishAsync()
    {
        if (Manager is null)
            throw new InvalidOperationException("Brak aktywnej gry.");

        if (!Manager.IsCompleted)
            Manager.Complete();
        LastReport = Manager.Report!;
        LastScore = _scoring.Score(LastReport);

        var record = new GameRecord(LastScore.Points, LastScore.MaxPoints, LastReport.NumberOfCompletedTasks,
            (long)LastReport.Duration.TotalMilliseconds, DateTime.UtcNow);
        // Gra za 0 punktów nie jest rekordem, nawet pierwsza albo szybsza.
        IsNewRecord = await _results.SaveAsync(record) && record.Points > 0;
        Best = await _results.GetBestAsync();
        return LastScore;
    }
}
