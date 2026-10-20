using System.Globalization;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Todo;

public enum TodoFilter { All, Overdue, Done }

public sealed class TodoItem
{
    public TodoItem(string title, DateOnly due, bool done = false)
    {
        Title = title;
        Due = due;
        Done = done;
    }

    public string Title { get; }
    public DateOnly Due { get; }
    public bool Done { get; internal set; }
}

/// <summary>Lista zadań z terminami: filtr zaległych, sortowanie po terminie, usuwanie.</summary>
public abstract class TodoSimulationBase : TaskBase
{
    public const int TitleMaxLength = 100;

    public const string Spec =
        "Lista zadań z terminami.\n\n" +
        "Wymagania:\n" +
        "• Filtr „Zaległe” pokazuje nieukończone zadania z terminem wcześniejszym niż dzisiejszy. Zadanie z terminem na dziś nie jest zaległe.\n" +
        "• Przycisk „Sortuj po terminie” układa zadania od najwcześniejszego terminu.\n" +
        "• Przycisk usuwania usuwa zadanie z tego samego wiersza, także przy włączonym filtrze.\n" +
        "• Lista pokazuje terminy w formacie dd.mm.rrrr.";

    /// <summary>Karta misji wspólna dla trzech zadań aplikacji.</summary>
    public static readonly MissionCharter AppCharter = new(
        "Sprawdź, czy filtr, sortowanie i usuwanie działają poprawnie dla zadań o różnych terminach.",
        new TestIdea[]
        {
            new("Granice czasu", "Dodaj zadania z terminem wczoraj, dziś i jutro, potem włącz filtr „Zaległe”.", DefectCategory.WartosciBrzegowe),
            new("Kolejność", "Dodaj terminy z różnych miesięcy i lat, potem posortuj listę.", DefectCategory.DatyICzas),
            new("Format", "Sprawdź, jak lista pokazuje terminy z jednocyfrowym dniem i miesiącem.", DefectCategory.FormatyILokalizacja),
            new("Akcja w widoku", "Usuń zadanie przy włączonym filtrze i sprawdź, które zniknęło.", DefectCategory.SpojnoscDanych),
            new("Złe dane", "Dodaj zadanie bez nazwy albo bez terminu.", DefectCategory.KlasyRownowaznosci),
        });

    public sealed override MissionCharter Charter => AppCharter;

    private readonly List<TodoItem> _items = new();

    protected TodoSimulationBase(uint number, PointValue points, DateOnly? today)
        : base(number, points, Spec)
    {
        Today = today ?? DateOnly.FromDateTime(DateTime.Now);
        OnReset();
    }

    public DateOnly Today { get; }
    public TodoFilter Filter { get; private set; }
    public IReadOnlyList<TodoItem> Items => _items;
    public IReadOnlyList<TodoItem> Visible => _items.Where(Matches).ToList();

    public void SetFilter(TodoFilter filter)
    {
        Filter = filter;
        CheckOverdueView();
    }

    public OperationResult Add(string? title, string? dueText)
    {
        var text = (title ?? string.Empty).Trim();
        if (text.Length == 0)
            return OperationResult.Fail("Podaj treść zadania.");
        if (text.Length > TitleMaxLength)
            return OperationResult.Fail($"Treść zadania może mieć najwyżej {TitleMaxLength} znaków.");
        if (!DateOnly.TryParseExact((dueText ?? string.Empty).Trim(), new[] { "yyyy-MM-dd", "dd.MM.yyyy" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var due))
            return OperationResult.Fail("Wybierz termin zadania.");
        _items.Add(new TodoItem(text, due));
        CheckOverdueView();
        return OperationResult.Ok;
    }

    public void ToggleDone(TodoItem item)
    {
        item.Done = !item.Done;
        CheckOverdueView();
    }

    public void SortByDue()
    {
        var sorted = _items.Order(Comparer<TodoItem>.Create(CompareDue)).ToList();
        _items.Clear();
        _items.AddRange(sorted);
        // Kolejność niechronologiczna po sortowaniu.
        if (_items.Zip(_items.Skip(1)).Any(p => p.First.Due > p.Second.Due))
            LogDefectVisible();
    }

    /// <summary>Usuwa zadanie z wiersza o podanym numerze na widocznej (przefiltrowanej) liście.</summary>
    public void DeleteVisibleAt(int index)
    {
        var visible = Visible;
        if (index < 0 || index >= visible.Count)
            return;
        var target = ResolveDeleteTarget(index, visible);
        _items.Remove(target);
        if (!ReferenceEquals(target, visible[index]))
            LogDefectVisible(); // zniknęło inne zadanie niż kliknięte
    }

    protected virtual bool IsOverdue(TodoItem item) => !item.Done && item.Due < Today;

    protected virtual int CompareDue(TodoItem a, TodoItem b) => a.Due.CompareTo(b.Due);

    protected virtual TodoItem ResolveDeleteTarget(int visibleIndex, IReadOnlyList<TodoItem> visible) =>
        visible[visibleIndex];

    private bool Matches(TodoItem item) => Filter switch
    {
        TodoFilter.Overdue => IsOverdue(item),
        TodoFilter.Done => item.Done,
        _ => true,
    };

    private void CheckOverdueView()
    {
        // Wśród zaległych jest zadanie, którego termin jeszcze nie minął.
        if (Filter == TodoFilter.Overdue && Visible.Any(i => i.Due >= Today))
            LogDefectVisible();
    }

    protected override void OnReset()
    {
        Filter = TodoFilter.All;
        _items.Clear();
        _items.AddRange(new[]
        {
            new TodoItem("Testy wydajności koszyka", Today.AddDays(24)),
            new TodoItem("Retest poprawki logowania", Today.AddDays(-2)),
            new TodoItem("Plan testów na kolejny sprint", Today.AddDays(40)),
            new TodoItem("Raport z testów regresji", Today),
            new TodoItem("Przegląd przypadków testowych", Today.AddDays(3)),
            new TodoItem("Zgłoszenie defektu w raportach", Today.AddDays(-9), done: true),
        });
    }
}
