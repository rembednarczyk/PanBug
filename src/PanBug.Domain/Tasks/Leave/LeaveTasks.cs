using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Leave;

/// <summary>Zadanie 3: soboty i niedziele są liczone jako dni urlopu.</summary>
public sealed class Task03Leave : LeaveSimulationBase
{
    public Task03Leave(DateOnly? today = null) : base(3, PointValue.One, today) { }

    protected override IReadOnlyDictionary<int, int> CountDays(DateOnly from, DateOnly to) =>
        DaysBetween(from, to).GroupBy(d => d.Year).ToDictionary(g => g.Key, g => g.Count());
}

/// <summary>Zadanie 16: wniosek przez przełom roku pobiera wszystkie dni z puli roku początkowego.</summary>
public sealed class Task16Leave : LeaveSimulationBase
{
    public Task16Leave(DateOnly? today = null) : base(16, PointValue.Three, today) { }

    protected override IReadOnlyDictionary<int, int> CountDays(DateOnly from, DateOnly to) =>
        new Dictionary<int, int> { [from.Year] = WorkingDaysByYear(from, to).Values.Sum() };
}

/// <summary>Zadanie 22: anulowanie zaakceptowanego wniosku nie zwraca dni do puli.</summary>
public sealed class Task22Leave : LeaveSimulationBase
{
    public Task22Leave(DateOnly? today = null) : base(22, PointValue.Two, today) { }

    protected override bool ReturnsDaysOnCancel(bool wasApproved) => !wasApproved;
}
