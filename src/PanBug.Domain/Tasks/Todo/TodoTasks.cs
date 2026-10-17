using System.Globalization;
using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Todo;

/// <summary>Zadanie 16: zadanie z terminem na dziś trafia do zaległych ("do dziś włącznie" zamiast "przed dziś").</summary>
public sealed class Task16Todo : TodoSimulationBase
{
    public Task16Todo(DateOnly? today = null) : base(16, PointValue.Two, today) { }

    protected override bool IsOverdue(TodoItem item) => !item.Done && item.Due <= Today;
}

/// <summary>Zadanie 17: sortowanie porównuje terminy jako tekst dd.mm.rrrr, więc najpierw decyduje dzień miesiąca.</summary>
public sealed class Task17Todo : TodoSimulationBase
{
    public Task17Todo(DateOnly? today = null) : base(17, PointValue.Two, today) { }

    protected override int CompareDue(TodoItem a, TodoItem b) =>
        string.CompareOrdinal(Text(a.Due), Text(b.Due));

    private static string Text(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}

/// <summary>Zadanie 18: usuwanie przy włączonym filtrze trafia w wiersz o tym samym numerze na pełnej liście.</summary>
public sealed class Task18Todo : TodoSimulationBase
{
    public Task18Todo(DateOnly? today = null) : base(18, PointValue.Four, today) { }

    protected override TodoItem ResolveDeleteTarget(int visibleIndex, IReadOnlyList<TodoItem> visible) =>
        Items[visibleIndex];
}
