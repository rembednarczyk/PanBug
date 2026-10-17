using System.Diagnostics;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks;

namespace PanBug.Domain.Session;

public sealed class TaskEventArgs : EventArgs
{
    public int Index { get; }
    public TaskSlot Slot { get; }
    public TaskEventArgs(int index, TaskSlot slot) { Index = index; Slot = slot; }
}

/// <summary>
/// Zarządza przebiegiem sesji 24 zadań: nawigacją, stoperem, ukończeniami
/// i budową końcowego raportu. Nie zależy od UI.
/// </summary>
public sealed class TaskManager
{
    private readonly Stopwatch _stopwatch = new();

    public IReadOnlyList<TaskSlot> Tasks { get; }
    public int CurrentIndex { get; private set; }
    public int CompletedCount { get; private set; }
    public bool IsCompleted { get; private set; }
    public int MaxPoints { get; }
    public TimeSpan Elapsed => _stopwatch.Elapsed;
    public Report? Report { get; private set; }

    public event EventHandler<TaskEventArgs>? TaskStarted;
    public event EventHandler<TaskEventArgs>? TaskEnded;
    public event EventHandler? Completed;

    public TaskManager(IReadOnlyList<ITaskSimulation> simulations)
    {
        if (simulations.Count == 0)
            throw new ArgumentException("Brak zadań.", nameof(simulations));
        Tasks = simulations.Select(s => new TaskSlot(s)).ToList();
        MaxPoints = Tasks.Sum(t => (int)t.Simulation.Points);
        CurrentIndex = 0;
    }

    public TaskSlot Current => Tasks[CurrentIndex];

    public void Run()
    {
        _stopwatch.Start();
        StartTask(0);
    }

    /// <summary>
    /// Przechodzi do zadania. Bieżące zadanie zostaje bez zmian: kliknięcie jego kafelka albo
    /// "Następne"/"Poprzednie", gdy zostało ostatnie zadanie, nie może skasować postępu gracza.
    /// Celowy restart to <see cref="ResetTask"/>.
    /// </summary>
    public TaskSlot RunTask(int index)
    {
        if (index < 0 || index >= Tasks.Count || Tasks[index].IsCompleted || index == CurrentIndex)
            return Current;
        return StartTask(index);
    }

    private TaskSlot StartTask(int index)
    {
        CurrentIndex = index;
        Tasks[index].Simulation.Reset();
        TaskStarted?.Invoke(this, new TaskEventArgs(index, Tasks[index]));
        return Current;
    }

    public TaskSlot ResetTask()
    {
        var slot = Current;
        if (!slot.IsCompleted)
        {
            slot.Simulation.Reset();
            TaskStarted?.Invoke(this, new TaskEventArgs(CurrentIndex, slot));
        }
        return slot;
    }

    public int GetNextTaskIndex()
    {
        int i = CurrentIndex, guard = 0;
        while (guard++ < Tasks.Count)
        {
            i = (i + 1) % Tasks.Count;
            if (!Tasks[i].IsCompleted)
                return i;
        }
        return -1;
    }

    public int GetPrevTaskIndex()
    {
        int i = CurrentIndex, guard = 0;
        while (guard++ < Tasks.Count)
        {
            i = (i - 1 + Tasks.Count) % Tasks.Count;
            if (!Tasks[i].IsCompleted)
                return i;
        }
        return -1;
    }

    public TaskSlot RunNextTask() => RunTask(GetNextTaskIndex());
    public TaskSlot RunPrevTask() => RunTask(GetPrevTaskIndex());

    /// <summary>
    /// Zgłasza defekt dla bieżącego zadania wraz z wybranymi kategoriami,
    /// przechwytuje log akcji i oznacza zadanie jako ukończone.
    /// </summary>
    public void CompleteCurrentTask(IReadOnlyList<uint> selectedCategories)
    {
        if (selectedCategories.Count == 0)
            throw new ArgumentException("Musisz wybrać przynajmniej jedną kategorię.", nameof(selectedCategories));

        var slot = Current;
        if (IsCompleted || slot.IsCompleted)
            return;

        slot.CapturedActions = slot.Simulation.Actions.ToArray();
        slot.SelectedAnswerNumbers = selectedCategories.ToArray();
        slot.IsCompleted = true;
        CompletedCount++;
        TaskEnded?.Invoke(this, new TaskEventArgs(CurrentIndex, slot));

        if (CompletedCount == Tasks.Count)
            Complete();
        else
            RunNextTask();
    }

    /// <summary>Kończy sesję, zatrzymuje stoper i buduje raport.</summary>
    public void Complete()
    {
        if (IsCompleted)
            return;
        _stopwatch.Stop();
        IsCompleted = true;
        Report = BuildReport();
        Completed?.Invoke(this, EventArgs.Empty);
    }

    public Report BuildReport()
    {
        var reportTasks = Tasks.Select(slot =>
        {
            var rt = new ReportTask(slot.Number) { IsCompleted = slot.IsCompleted };
            if (slot.IsCompleted)
            {
                rt.AnswerNumbers = slot.SelectedAnswerNumbers;
                rt.Actions = slot.CapturedActions;
            }
            return rt;
        }).ToList();

        return new Report(_stopwatch.Elapsed, reportTasks);
    }
}
