using System.Globalization;
using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Todo;

/// <summary>Zadanie 10: zadanie z terminem na dziś trafia do zaległych ("do dziś włącznie" zamiast "przed dziś").</summary>
public sealed class Task10Todo : TodoSimulationBase
{
    public Task10Todo(DateOnly? today = null) : base(10, PointValue.Two, today) { }

    protected override bool IsOverdue(TodoItem item) => !item.Done && item.Due <= Today;
}

/// <summary>Zadanie 15: sortowanie porównuje terminy jako tekst dd.mm.rrrr, więc najpierw decyduje dzień miesiąca.</summary>
public sealed class Task15Todo : TodoSimulationBase
{
    public Task15Todo(DateOnly? today = null) : base(15, PointValue.Two, today) { }

    protected override int CompareDue(TodoItem a, TodoItem b) =>
        string.CompareOrdinal(Text(a.Due), Text(b.Due));

    private static string Text(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}

/// <summary>Zadanie 23: usuwanie przy włączonym filtrze trafia w wiersz o tym samym numerze na pełnej liście.</summary>
public sealed class Task23Todo : TodoSimulationBase
{
    public Task23Todo(DateOnly? today = null) : base(23, PointValue.Four, today) { }

    protected override TodoItem ResolveDeleteTarget(int visibleIndex, IReadOnlyList<TodoItem> visible) =>
        Items[visibleIndex];
}
