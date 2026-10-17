using PanBug.Domain.Model;
using PanBug.Domain.Tasks;
using PanBug.Domain.Tasks.Cinema;
using Xunit;

namespace PanBug.Domain.Tests;

public class CinemaTests
{
    internal static readonly DateOnly Today = new(2026, 10, 14);

    private sealed class CorrectCinema : CinemaSimulationBase
    {
        public CorrectCinema() : base(99, PointValue.One, CinemaTests.Today) { }
    }

    /// <summary>Kroki zadania 1: bilet dla 12-latka.</summary>
    private static void TwelveYearOld(CinemaSimulationBase sim)
    {
        sim.SelectShowing(sim.ShowingsFor(Today)[0]);
        Assert.True(sim.ToggleSeat(new Seat(1, 1)).Success);
        sim.Tickets[0].Age = "12";
        Assert.True(sim.Summarize().Success);
    }

    /// <summary>Kroki zadania 2: zaznacz, odznacz i ponownie zaznacz to samo miejsce.</summary>
    private static void ReselectSeat(CinemaSimulationBase sim)
    {
        sim.SelectShowing(sim.ShowingsFor(Today)[0]);
        sim.ToggleSeat(new Seat(2, 2));
        sim.ToggleSeat(new Seat(2, 2));
        sim.ToggleSeat(new Seat(2, 2));
        foreach (var ticket in sim.Tickets)
            ticket.Age = "30";
        Assert.True(sim.Summarize().Success);
    }

    /// <summary>Kroki zadania 3: każdy seans z listy każdego dnia.</summary>
    private static void OpenEveryShowing(CinemaSimulationBase sim)
    {
        foreach (var day in sim.Days)
        {
            sim.SelectDay(day);
            foreach (var showing in sim.ShowingsFor(day))
            {
                sim.SelectShowing(showing);
                sim.BackToShowings();
            }
        }
    }

    [Fact]
    public void Task01_twelve_year_old_pays_full_price()
    {
        var sim = new Task01Cinema(Today);
        TwelveYearOld(sim);
        Assert.True(sim.DefectShown);
        Assert.Equal(CinemaSimulationBase.NormalPrice, sim.Summary!.Total);
    }

    [Fact]
    public void Correct_twelve_year_old_gets_reduced_ticket()
    {
        var sim = new CorrectCinema();
        TwelveYearOld(sim);
        Assert.False(sim.DefectShown);
        Assert.Equal(CinemaSimulationBase.ReducedPrice, sim.Summary!.Total);
    }

    [Fact]
    public void Task02_reselected_seat_is_billed_twice()
    {
        var sim = new Task02Cinema(Today);
        ReselectSeat(sim);
        Assert.True(sim.DefectShown);
        Assert.Equal(2, sim.Summary!.Lines.Count);
        Assert.Single(sim.SelectedSeats);
    }

    [Fact]
    public void Correct_reselected_seat_is_one_ticket()
    {
        var sim = new CorrectCinema();
        ReselectSeat(sim);
        Assert.False(sim.DefectShown);
        Assert.Single(sim.Summary!.Lines);
    }

    [Fact]
    public void Task03_night_showing_listed_on_previous_day()
    {
        var sim = new Task03Cinema(Today);
        var night = sim.ShowingsFor(Today).Single(s => s.Start.Date > Today.ToDateTime(TimeOnly.MinValue));
        Assert.Equal(Today.AddDays(1), DateOnly.FromDateTime(night.Start));
        OpenEveryShowing(sim);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_lists_every_showing_on_its_start_day()
    {
        var sim = new CorrectCinema();
        OpenEveryShowing(sim);
        Assert.False(sim.DefectShown);
        Assert.Contains(sim.ShowingsFor(Today.AddDays(1)), s => s.Start.Hour == 0);
    }

    [Fact]
    public void Seat_limit_and_occupied_seats_are_enforced()
    {
        var sim = new CorrectCinema();
        sim.SelectShowing(sim.ShowingsFor(Today)[0]);
        Assert.False(sim.ToggleSeat(new Seat(1, 4)).Success); // zajęte
        for (int n = 1; n <= CinemaSimulationBase.MaxSeats; n++)
            Assert.True(sim.ToggleSeat(new Seat(4, n)).Success);
        Assert.False(sim.ToggleSeat(new Seat(4, 7)).Success);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("121")]
    [InlineData("")]
    [InlineData("1a")]
    public void Age_outside_range_is_rejected(string age)
    {
        var sim = new CorrectCinema();
        sim.SelectShowing(sim.ShowingsFor(Today)[0]);
        sim.ToggleSeat(new Seat(1, 1));
        sim.Tickets[0].Age = age;
        Assert.False(sim.Summarize().Success);
    }

    [Fact]
    public void Booking_reports_total()
    {
        var sim = new CorrectCinema();
        sim.SelectShowing(sim.ShowingsFor(Today)[0]);
        sim.ToggleSeat(new Seat(1, 1));
        sim.ToggleSeat(new Seat(1, 2));
        sim.Tickets[0].Age = "8";
        sim.Tickets[1].Age = "40";
        sim.Summarize();
        var result = sim.Book();
        Assert.True(result.Success);
        Assert.Contains("46,00 zł", result.Message);
        Assert.Equal(CinemaStage.Booked, sim.Stage);
    }
}
