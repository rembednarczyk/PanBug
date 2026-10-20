using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks;

/// <summary>Wspólna baza symulacji zadań: numer, punkty, opis, log akcji.</summary>
public abstract class TaskBase : ITaskSimulation
{
    /// <summary>
    /// Kod akcji potwierdzający odtworzenie defektu. Symulacja zapisuje go dopiero wtedy,
    /// gdy defekt jest widoczny na ekranie, więc samo podejrzenie defektu nie daje punktów.
    /// </summary>
    public const uint DefectMarker = 100;

    private readonly ActionLog _actionLog = new();

    protected TaskBase(uint number, PointValue points, string description)
    {
        Number = number;
        Points = points;
        Description = description;
    }

    public uint Number { get; }
    public PointValue Points { get; }
    public string Description { get; }
    public abstract MissionCharter Charter { get; }
    public IReadOnlyList<Answer> Categories => DefectCategories.All;
    public IReadOnlyList<uint> Actions => _actionLog.Actions;

    /// <summary>Czy w bieżącym przebiegu defekt został już pokazany graczowi.</summary>
    public bool DefectShown => _actionLog.Actions.Contains(DefectMarker);

    protected void LogAction(uint code) => _actionLog.Log(code);
    protected void LogDefectVisible() => _actionLog.Log(DefectMarker);
    protected void ClearActions() => _actionLog.Clear();

    public void Reset()
    {
        ClearActions();
        OnReset();
    }

    /// <summary>Resetuje stan specyficzny dla zadania.</summary>
    protected abstract void OnReset();
}
