using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Leave;

/// <summary>Zadanie 10: soboty i niedziele są liczone jako dni urlopu.</summary>
public sealed class Task10Leave : LeaveSimulationBase
{
    public Task10Leave(DateOnly? today = null) : base(10, PointValue.One, today) { }

    protected override IReadOnlyDictionary<int, int> CountDays(DateOnly from, DateOnly to) =>
        DaysBetween(from, to).GroupBy(d => d.Year).ToDictionary(g => g.Key, g => g.Count());
}

/// <summary>Zadanie 11: wniosek przez przełom roku pobiera wszystkie dni z puli roku początkowego.</summary>
public sealed class Task11Leave : LeaveSimulationBase
{
    public Task11Leave(DateOnly? today = null) : base(11, PointValue.Three, today) { }

    protected override IReadOnlyDictionary<int, int> CountDays(DateOnly from, DateOnly to) =>
        new Dictionary<int, int> { [from.Year] = WorkingDaysByYear(from, to).Values.Sum() };
}

/// <summary>Zadanie 12: anulowanie zaakceptowanego wniosku nie zwraca dni do puli.</summary>
public sealed class Task12Leave : LeaveSimulationBase
{
    public Task12Leave(DateOnly? today = null) : base(12, PointValue.Two, today) { }

    protected override bool ReturnsDaysOnCancel(bool wasApproved) => !wasApproved;
}
