using PanBug.Domain.Common;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Cinema;

public sealed record Showing(string Title, DateTime Start, string Hall);

public sealed record Seat(int Row, int Number)
{
    public override string ToString() => $"rząd {Row}, miejsce {Number}";
}

/// <summary>Bilet przed podsumowaniem: miejsce i wpisany wiek widza.</summary>
public sealed class TicketDraft
{
    public TicketDraft(Seat seat) => Seat = seat;
    public Seat Seat { get; }
    public string Age { get; set; } = string.Empty;
}

public sealed record TicketLine(Seat Seat, int Age, bool Reduced, decimal Price);

public sealed record CinemaSummary(Showing Showing, IReadOnlyList<TicketLine> Lines)
{
    public decimal Total => Lines.Sum(l => l.Price);
}

public enum CinemaStage { Showings, Seats, Summary, Booked }

/// <summary>Kino: wybór dnia i seansu, miejsc na sali i biletów z ulgą wiekową.</summary>
public abstract class CinemaSimulationBase : TaskBase
{
    public const int Rows = 6;
    public const int SeatsPerRow = 10;
    public const int MaxSeats = 6;
    public const int ReducedMaxAge = 12;
    public const decimal NormalPrice = 28m;
    public const decimal ReducedPrice = 18m;

    public const string Spec =
        "Kino „Ekran”: rezerwacja biletów na seans.\n\n" +
        "Wymagania:\n" +
        "• Lista dnia pokazuje seanse, które zaczynają się w tym dniu kalendarzowym (od 00:00 do 23:59).\n" +
        "• Jedna rezerwacja obejmuje od 1 do 6 miejsc. Każde zaznaczone miejsce to jeden bilet.\n" +
        "• Bilet ulgowy (18,00 zł) przysługuje widzom w wieku do 12 lat włącznie, pozostali płacą 28,00 zł.\n" +
        "• Wiek widza to liczba od 1 do 120.";

    /// <summary>Karta misji wspólna dla trzech zadań aplikacji.</summary>
    public static readonly MissionCharter AppCharter = new(
        "Sprawdź, czy rezerwacja liczy bilety i ceny zgodnie z wymaganiami i czy lista dnia pokazuje właściwe seanse.",
        new TestIdea[]
        {
            new("Granice", "Wypróbuj wartości na granicach z wymagań i tuż obok nich: wiek widza, liczba miejsc.", DefectCategory.WartosciBrzegowe),
            new("Złe dane", "Wpisz wiek, który nie ma sensu: 0, 121, puste pole, litery. Czy aplikacja go odrzuca?", DefectCategory.KlasyRownowaznosci),
            new("Zmiana zdania", "Zaznaczaj i odznaczaj miejsca, wracaj do listy seansów. Czy podsumowanie nadąża za wyborem?", DefectCategory.PrzejsciaStanow),
            new("Kalendarz", "Przejrzyj seanse w różne dni i o różnych porach, także późno w nocy.", DefectCategory.DatyICzas),
        });

    public sealed override MissionCharter Charter => AppCharter;

    private static readonly HashSet<Seat> Occupied = new()
    {
        new(1, 4), new(1, 5), new(2, 6), new(3, 1), new(3, 2), new(4, 8), new(5, 5), new(6, 9), new(6, 10),
    };

    private readonly List<Seat> _selectedSeats = new();
    private readonly List<TicketDraft> _tickets = new();
    private readonly List<Showing> _showings;

    protected CinemaSimulationBase(uint number, PointValue points, DateOnly? today)
        : base(number, points, Spec)
    {
        Today = today ?? DateOnly.FromDateTime(DateTime.Now);
        _showings = BuildShowings(Today);
        SelectedDay = Today;
    }

    public DateOnly Today { get; }
    public IReadOnlyList<DateOnly> Days => new[] { Today, Today.AddDays(1), Today.AddDays(2) };
    public DateOnly SelectedDay { get; private set; }
    public CinemaStage Stage { get; private set; }
    public Showing? Selected { get; private set; }
    public IReadOnlyList<Seat> SelectedSeats => _selectedSeats;
    public IReadOnlyList<TicketDraft> Tickets => _tickets;
    public CinemaSummary? Summary { get; private set; }

    public IReadOnlyList<Showing> ShowingsFor(DateOnly day) =>
        _showings.Where(s => ListingDayOf(s) == day).OrderBy(s => s.Start).ToList();

    public bool IsOccupied(Seat seat) => Occupied.Contains(seat);
    public bool IsSelected(Seat seat) => _selectedSeats.Contains(seat);

    public void SelectDay(DateOnly day)
    {
        SelectedDay = day;
        Stage = CinemaStage.Showings;
        Selected = null;
    }

    public void SelectShowing(Showing showing)
    {
        Selected = showing;
        _selectedSeats.Clear();
        _tickets.Clear();
        Summary = null;
        Stage = CinemaStage.Seats;
        // Seans otwarty z listy innego dnia niż dzień jego rozpoczęcia.
        if (DateOnly.FromDateTime(showing.Start) != SelectedDay)
            LogDefectVisible();
    }

    public OperationResult ToggleSeat(Seat seat)
    {
        if (Stage != CinemaStage.Seats)
            return OperationResult.Fail("Najpierw wybierz seans.");
        if (IsOccupied(seat))
            return OperationResult.Fail("To miejsce jest już zajęte.");

        if (_selectedSeats.Remove(seat))
        {
            OnSeatDeselected(seat);
            return OperationResult.Ok;
        }
        if (_selectedSeats.Count >= MaxSeats)
            return OperationResult.Fail($"Jedna rezerwacja może obejmować najwyżej {MaxSeats} miejsc.");
        _selectedSeats.Add(seat);
        _tickets.Add(new TicketDraft(seat));
        return OperationResult.Ok;
    }

    public OperationResult Summarize()
    {
        if (Selected is null || Stage != CinemaStage.Seats)
            return OperationResult.Fail("Najpierw wybierz seans.");
        if (_selectedSeats.Count == 0)
            return OperationResult.Fail("Zaznacz co najmniej jedno miejsce.");

        var lines = new List<TicketLine>();
        foreach (var ticket in _tickets)
        {
            if (!PolishNumber.TryParseInt(ticket.Age, out var age) || age < 1 || age > 120)
                return OperationResult.Fail($"Wiek widza musi być liczbą od 1 do 120 ({ticket.Seat}).");
            var reduced = IsReduced(age);
            lines.Add(new TicketLine(ticket.Seat, age, reduced, reduced ? ReducedPrice : NormalPrice));
        }

        Summary = new CinemaSummary(Selected, lines);
        Stage = CinemaStage.Summary;

        // Podsumowanie niezgodne z wymaganiami: więcej biletów niż miejsc albo 12-latek bez ulgi.
        if (lines.Count != _selectedSeats.Count || lines.Any(l => l.Age <= ReducedMaxAge && !l.Reduced))
            LogDefectVisible();
        return OperationResult.Ok;
    }

    public OperationResult Book()
    {
        if (Stage != CinemaStage.Summary || Summary is null)
            return OperationResult.Fail("Najpierw przejdź do podsumowania.");
        Stage = CinemaStage.Booked;
        return new OperationResult(true, $"Rezerwacja przyjęta. Do zapłaty: {PolishNumber.Money(Summary.Total)}.");
    }

    public void BackToSeats()
    {
        if (Stage == CinemaStage.Summary)
            Stage = CinemaStage.Seats;
    }

    public void BackToShowings() => SelectDay(SelectedDay);

    protected virtual DateOnly ListingDayOf(Showing showing) => DateOnly.FromDateTime(showing.Start);

    protected virtual bool IsReduced(int age) => age <= ReducedMaxAge;

    /// <summary>Odznaczenie miejsca usuwa jego bilet z zamówienia.</summary>
    protected virtual void OnSeatDeselected(Seat seat) =>
        _tickets.RemoveAll(t => t.Seat == seat);

    protected override void OnReset()
    {
        _selectedSeats.Clear();
        _tickets.Clear();
        Summary = null;
        Selected = null;
        SelectedDay = Today;
        Stage = CinemaStage.Showings;
    }

    private static List<Showing> BuildShowings(DateOnly today)
    {
        var list = new List<Showing>();
        for (int d = 0; d < 3; d++)
        {
            var day = today.AddDays(d).ToDateTime(TimeOnly.MinValue);
            list.Add(new Showing("Ostatni sprint", day.AddHours(14), "Sala 1"));
            list.Add(new Showing("Cisza w logach", day.AddHours(17).AddMinutes(30), "Sala 2"));
            list.Add(new Showing("Regresja", day.AddHours(20).AddMinutes(45), "Sala 1"));
            // Seans nocny zaczyna się po północy, czyli już następnego dnia kalendarzowego.
            if (d < 2)
                list.Add(new Showing("Nocny deploy", day.AddDays(1).AddMinutes(30), "Sala 3"));
        }
        return list;
    }
}
