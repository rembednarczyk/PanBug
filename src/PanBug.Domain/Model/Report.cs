namespace PanBug.Domain.Model;

/// <summary>Wpis raportu dla pojedynczego zadania.</summary>
public sealed class ReportTask
{
    public uint Number { get; }
    public bool IsCompleted { get; set; }

    /// <summary>Numery zgłoszonych kategorii defektu (1-15).</summary>
    public IReadOnlyList<uint> AnswerNumbers { get; set; } = Array.Empty<uint>();

    /// <summary>Log kodów akcji wykonanych przez gracza w zadaniu.</summary>
    public IReadOnlyList<uint> Actions { get; set; } = Array.Empty<uint>();

    public ReportTask(uint number) => Number = number;
}

/// <summary>Niezmienny raport z sesji - podstawa punktacji.</summary>
public sealed class Report
{
    public TimeSpan Duration { get; }
    public IReadOnlyList<ReportTask> Tasks { get; }
    public int NumberOfCompletedTasks { get; }

    public Report(TimeSpan duration, IReadOnlyList<ReportTask> tasks)
    {
        Duration = duration;
        Tasks = tasks;
        NumberOfCompletedTasks = tasks.Count(t => t.IsCompleted);
    }
}
