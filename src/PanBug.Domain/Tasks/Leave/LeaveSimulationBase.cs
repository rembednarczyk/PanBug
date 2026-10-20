using System.Globalization;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Leave;

public enum LeaveStatus { Pending, Approved, Cancelled }

public sealed class LeaveRequest
{
    internal LeaveRequest(int id, DateOnly from, DateOnly to, IReadOnlyDictionary<int, int> daysByYear)
    {
        Id = id;
        From = from;
        To = to;
        DaysByYear = daysByYear;
    }

    public int Id { get; }
    public DateOnly From { get; }
    public DateOnly To { get; }
    public IReadOnlyDictionary<int, int> DaysByYear { get; }
    public int Days => DaysByYear.Values.Sum();
    public LeaveStatus Status { get; internal set; }
}

/// <summary>Planer urlopów: wnioski, pule dni na dwa lata, akceptacja i anulowanie.</summary>
public abstract class LeaveSimulationBase : TaskBase
{
    public const int AnnualDays = 26;

    public const string Spec =
        "Planer urlopów (konto pracownika).\n\n" +
        "Wymagania:\n" +
        "• Na każdy rok przysługuje 26 dni urlopu. Wniosek może dotyczyć bieżącego i następnego roku.\n" +
        "• Do urlopu liczą się wyłącznie dni robocze, od poniedziałku do piątku (planer nie uwzględnia świąt).\n" +
        "• Wniosek obejmujący przełom roku pobiera dni z puli tego roku, do którego należą.\n" +
        "• Dni są rezerwowane z puli przy złożeniu wniosku. Anulowanie wniosku, oczekującego lub zaakceptowanego, zwraca dni do puli.\n" +
        "• Przycisk „Zaakceptuj” symuluje decyzję przełożonego.";

    /// <summary>Karta misji wspólna dla trzech zadań aplikacji.</summary>
    public static readonly MissionCharter AppCharter = new(
        "Sprawdź, czy planer liczy dni urlopu i pule obu lat zgodnie z wymaganiami.",
        new TestIdea[]
        {
            new("Kalendarz", "Złóż wnioski obejmujące weekend, cały tydzień i przełom roku.", DefectCategory.DatyICzas),
            new("Granice", "Wykorzystaj pulę co do dnia, a potem spróbuj wziąć jeszcze jeden.", DefectCategory.WartosciBrzegowe),
            new("Złe dane", "Podaj datę końca wcześniejszą niż początek albo datę z zeszłego roku.", DefectCategory.KlasyRownowaznosci),
            new("Cykl życia", "Przeprowadź wniosek przez wszystkie stany: złożony, zaakceptowany, anulowany. Po każdym kroku sprawdź pulę.", DefectCategory.PrzejsciaStanow),
        });

    public sealed override MissionCharter Charter => AppCharter;

    private readonly Dictionary<int, int> _used = new();
    private readonly List<LeaveRequest> _requests = new();

    protected LeaveSimulationBase(uint number, PointValue points, DateOnly? today)
        : base(number, points, Spec)
    {
        Today = today ?? DateOnly.FromDateTime(DateTime.Now);
        OnReset();
    }

    public DateOnly Today { get; }
    public IReadOnlyList<int> Years => new[] { Today.Year, Today.Year + 1 };
    public IReadOnlyList<LeaveRequest> Requests => _requests;

    public int Used(int year) => _used.GetValueOrDefault(year);
    public int Remaining(int year) => AnnualDays - Used(year);

    public OperationResult Submit(string fromText, string toText)
    {
        if (!TryParseDate(fromText, out var from) || !TryParseDate(toText, out var to))
            return OperationResult.Fail("Podaj obie daty urlopu.");
        if (to < from)
            return OperationResult.Fail("Data końcowa nie może być wcześniejsza niż początkowa.");
        if (from.Year < Today.Year || to.Year > Today.Year + 1)
            return OperationResult.Fail($"Wniosek może dotyczyć tylko lat {Today.Year} i {Today.Year + 1}.");

        var days = CountDays(from, to);
        if (days.Values.Sum() == 0)
            return OperationResult.Fail("Wybrany okres nie obejmuje dni roboczych.");
        foreach (var (year, count) in days)
        {
            if (count > Remaining(year))
                return OperationResult.Fail(
                    $"Brakuje dni urlopu w puli na rok {year}: dostępne {Remaining(year)}, potrzebne {count}.");
        }

        foreach (var (year, count) in days)
            _used[year] = Used(year) + count;
        _requests.Add(new LeaveRequest(_requests.Count + 1, from, to, days));

        // Dni pobrane z puli inaczej, niż wynika z dni roboczych w każdym roku.
        var expected = WorkingDaysByYear(from, to);
        if (days.Count != expected.Count || days.Any(d => expected.GetValueOrDefault(d.Key) != d.Value))
            LogDefectVisible();

        return new OperationResult(true, $"Wniosek złożony. Liczba dni urlopu: {days.Values.Sum()}.");
    }

    public void Approve(LeaveRequest request)
    {
        if (request.Status == LeaveStatus.Pending)
            request.Status = LeaveStatus.Approved;
    }

    public void Cancel(LeaveRequest request)
    {
        if (request.Status == LeaveStatus.Cancelled)
            return;
        var wasApproved = request.Status == LeaveStatus.Approved;
        request.Status = LeaveStatus.Cancelled;
        if (ReturnsDaysOnCancel(wasApproved))
        {
            foreach (var (year, count) in request.DaysByYear)
                _used[year] = Used(year) - count;
        }
        else
        {
            LogDefectVisible(); // anulowany wniosek nadal zajmuje dni w puli
        }
    }

    /// <summary>Dni urlopu wniosku z podziałem na lata.</summary>
    protected virtual IReadOnlyDictionary<int, int> CountDays(DateOnly from, DateOnly to) =>
        WorkingDaysByYear(from, to);

    protected virtual bool ReturnsDaysOnCancel(bool wasApproved) => true;

    protected static Dictionary<int, int> WorkingDaysByYear(DateOnly from, DateOnly to) =>
        DaysBetween(from, to).Where(IsWorkingDay)
            .GroupBy(d => d.Year).ToDictionary(g => g.Key, g => g.Count());

    protected static IEnumerable<DateOnly> DaysBetween(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }

    protected static bool IsWorkingDay(DateOnly day) =>
        day.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    private static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact((text ?? string.Empty).Trim(), new[] { "yyyy-MM-dd", "dd.MM.yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    protected override void OnReset()
    {
        _used.Clear();
        _requests.Clear();
    }
}
