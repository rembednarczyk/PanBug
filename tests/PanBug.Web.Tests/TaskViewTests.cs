using AngleSharp.Dom;
using Bunit;
using PanBug.Domain.Tasks;
using PanBug.Domain.Tasks.Bill;
using PanBug.Domain.Tasks.Cinema;
using PanBug.Domain.Tasks.Delivery;
using PanBug.Domain.Tasks.Leave;
using PanBug.Domain.Tasks.Loan;
using PanBug.Domain.Tasks.Login;
using PanBug.Domain.Tasks.Parcel;
using PanBug.Domain.Tasks.Todo;
using PanBug.Web.Tasks;
using Xunit;

namespace PanBug.Web.Tests;

/// <summary>
/// Każdy defekt odtworzony przez widok (kliknięcia i wpisywanie jak gracz) zapisuje marker
/// w symulacji. Test sprawdza powiązanie widoku z symulacją, a nie samą logikę domeny.
/// </summary>
public class TaskViewTests : WebTestBase
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private static void Click(IRenderedFragment cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Trim() == text).Click();

    private static IElement Input(IRenderedFragment cut, int index) =>
        cut.FindAll("input:not([type=checkbox])")[index];

    private static bool Shown(ITaskSimulation sim) => sim.Actions.Contains(TaskBase.DefectMarker);

    // ---------- A. Kino ----------

    private IRenderedComponent<CinemaTask> Cinema(CinemaSimulationBase sim) =>
        RenderComponent<CinemaTask>(p => p.Add(c => c.Simulation, sim));

    [Fact]
    public void Task01_twelve_year_old_without_reduced_ticket()
    {
        var sim = new Task01Cinema();
        var cut = Cinema(sim);
        Click(cut, "Wybierz miejsca");
        cut.Find("button[aria-label='Rząd 1, miejsce 1']").Click();
        Input(cut, 0).Change("12");
        Click(cut, "Podsumowanie");
        Assert.Contains("28,00 zł", cut.Find("tfoot").TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task09_reselected_seat_shows_two_tickets()
    {
        var sim = new Task09Cinema();
        var cut = Cinema(sim);
        Click(cut, "Wybierz miejsca");
        for (int i = 0; i < 3; i++)
            cut.Find("button[aria-label='Rząd 1, miejsce 1']").Click();
        Assert.Equal(2, cut.FindAll("input.num").Count);
        cut.FindAll("input.num")[0].Change("30");
        cut.FindAll("input.num")[1].Change("30");
        Click(cut, "Podsumowanie");
        Assert.Contains("2 bilety", cut.Find("tfoot").TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task18_night_showing_opens_with_next_day_date()
    {
        var sim = new Task18Cinema();
        var cut = Cinema(sim);
        var row = cut.FindAll("tbody tr").First(r => r.TextContent.Contains("00:30"));
        row.QuerySelector("button")!.Click();
        Assert.Contains(Today.AddDays(1).ToString("dd.MM.yyyy"), cut.Find("dl.details").TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Correct_cinema_lists_night_showing_on_next_day()
    {
        var sim = new Task01Cinema();
        var cut = Cinema(sim);
        Assert.DoesNotContain("00:30", cut.Find("tbody").TextContent);
        cut.FindAll(".segmented button")[1].Click();
        Assert.Contains("00:30", cut.Find("tbody").TextContent);
    }

    // ---------- B. Automat paczkowy ----------

    private IRenderedComponent<ParcelTask> Parcel(ParcelSimulationBase sim) =>
        RenderComponent<ParcelTask>(p => p.Add(c => c.Simulation, sim));

    private static void Send(IRenderedFragment cut, string height, string phone)
    {
        Input(cut, 0).Change(height);
        Input(cut, 1).Change("30");
        Input(cut, 2).Change("30");
        Input(cut, 3).Change("2,5");
        Input(cut, 4).Change(phone);
        Click(cut, "Nadaj paczkę");
    }

    [Fact]
    public void Task02_parcel_at_height_limit_is_rejected()
    {
        var sim = new Task02Parcel();
        var cut = Parcel(sim);
        Send(cut, "64", "600000000");
        Assert.Contains("Wysokość musi", cut.Markup);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task06_code_with_leading_zero_is_refused()
    {
        var sim = new Task06Parcel();
        var cut = Parcel(sim);
        Send(cut, "30", "600000000");
        Assert.Contains("048213", cut.Find("table.grid").TextContent);
        Input(cut, 5).Change("600000000");
        Input(cut, 6).Change("048213");
        Click(cut, "Otwórz skrytkę");
        Assert.Contains("Nie znaleziono paczki", cut.Markup);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task13_phone_from_placeholder_is_rejected()
    {
        var sim = new Task13Parcel();
        var cut = Parcel(sim);
        Assert.Equal("+48 600 000 000", Input(cut, 4).GetAttribute("placeholder"));
        Send(cut, "30", "+48 600 000 000");
        Assert.True(Shown(sim));
    }

    // ---------- C. Kalkulator kredytu ----------

    private IRenderedComponent<LoanTask> Loan(LoanSimulationBase sim, string amount, string rate, string period)
    {
        var cut = RenderComponent<LoanTask>(p => p.Add(c => c.Simulation, sim));
        Input(cut, 0).Change(amount);
        Input(cut, 1).Change(rate);
        Input(cut, 2).Input(period);
        return cut;
    }

    [Fact]
    public void Task07_zero_rate_shows_nan()
    {
        var sim = new Task07Loan();
        var cut = Loan(sim, "12 000", "0", "12");
        Click(cut, "Oblicz ratę");
        Assert.Contains("NaN", cut.Find("dd.output").TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task14_schedule_does_not_end_at_zero()
    {
        var sim = new Task14Loan();
        var cut = Loan(sim, "10 000", "7,5", "12");
        Click(cut, "Oblicz ratę");
        Assert.DoesNotContain("0,00 zł", cut.FindAll("tbody tr")[^1].Children[^1].TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task20_unit_change_keeps_months_value()
    {
        var sim = new Task20Loan();
        var cut = Loan(sim, "10 000", "6", "24");
        cut.Find("select").Change(nameof(PeriodUnit.Years));
        Assert.Equal("24", Input(cut, 2).GetAttribute("value"));
        Click(cut, "Oblicz ratę");
        Assert.Contains("288 miesięcy", cut.Markup);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Correct_unit_change_converts_value_in_the_field()
    {
        var sim = new Task07Loan();
        var cut = Loan(sim, "10 000", "6", "24");
        cut.Find("select").Change(nameof(PeriodUnit.Years));
        Assert.Equal("2", Input(cut, 2).GetAttribute("value"));
    }

    // ---------- D. Planer urlopów ----------

    private IRenderedComponent<LeaveTask> Leave(LeaveSimulationBase sim, DateOnly from, DateOnly to)
    {
        var cut = RenderComponent<LeaveTask>(p => p.Add(c => c.Simulation, sim));
        Input(cut, 0).Change(from.ToString("yyyy-MM-dd"));
        Input(cut, 1).Change(to.ToString("yyyy-MM-dd"));
        Click(cut, "Złóż wniosek");
        return cut;
    }

    private static DateOnly NextYearMonday()
    {
        var day = new DateOnly(Today.Year + 1, 1, 10);
        while (day.DayOfWeek != DayOfWeek.Monday)
            day = day.AddDays(1);
        return day;
    }

    [Fact]
    public void Task03_weekend_is_counted_as_leave()
    {
        var sim = new Task03Leave();
        var monday = NextYearMonday();
        Leave(sim, monday, monday.AddDays(6));
        Assert.Equal(7, sim.Used(Today.Year + 1));
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task16_year_end_request_takes_days_from_one_pool()
    {
        var sim = new Task16Leave();
        var cut = Leave(sim, new DateOnly(Today.Year, 12, 28), new DateOnly(Today.Year + 1, 1, 8));
        Assert.DoesNotContain($"{Today.Year + 1}:", cut.FindAll("table.grid")[1].TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task22_cancelled_approved_request_keeps_days()
    {
        var sim = new Task22Leave();
        var monday = NextYearMonday();
        var cut = Leave(sim, monday, monday.AddDays(4));
        Click(cut, "Zaakceptuj");
        Click(cut, "Anuluj");
        Assert.Contains("anulowany", cut.Markup);
        Assert.Equal(5, sim.Used(Today.Year + 1));
        Assert.True(Shown(sim));
    }

    // ---------- E. Logowanie ----------

    private IRenderedComponent<LoginTask> Login(LoginSimulationBase sim) =>
        RenderComponent<LoginTask>(p => p.Add(c => c.Simulation, sim));

    private static void LogIn(IRenderedFragment cut, string login, string password)
    {
        Input(cut, 0).Change(login);
        Input(cut, 1).Change(password);
        Click(cut, "Zaloguj");
    }

    [Fact]
    public void Task05_reset_reveals_existing_account()
    {
        var sim = new Task05Login();
        var cut = Login(sim);
        Click(cut, "Nie pamiętam hasła");
        Input(cut, 0).Change("nikt@example.pl");
        Click(cut, "Wyślij link");
        Assert.Contains("Nie znaleziono konta", cut.Markup);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task12_capital_letter_bypasses_lock()
    {
        var sim = new Task12Login();
        var cut = Login(sim);
        for (int i = 0; i < 3; i++)
            LogIn(cut, "tester", "zle");
        Assert.Contains("zablokowane", cut.Markup);
        LogIn(cut, "Tester", LoginSimulationBase.AccountPassword);
        Assert.Contains("Zalogowano jako tester", cut.Markup);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task19_password_left_in_form_after_logout()
    {
        var sim = new Task19Login();
        var cut = Login(sim);
        LogIn(cut, "tester", LoginSimulationBase.AccountPassword);
        Click(cut, "Wyloguj");
        Assert.Equal(LoginSimulationBase.AccountPassword, cut.Find("input[type=password]").GetAttribute("value"));
        Assert.True(Shown(sim));
    }

    // ---------- F. Lista zadań ----------

    private IRenderedComponent<TodoTask> Todo(TodoSimulationBase sim) =>
        RenderComponent<TodoTask>(p => p.Add(c => c.Simulation, sim));

    [Fact]
    public void Task10_task_due_today_in_overdue_filter()
    {
        var sim = new Task10Todo();
        var cut = Todo(sim);
        Click(cut, "Zaległe");
        Assert.Contains(Today.ToString("dd.MM.yyyy"), cut.Find("tbody").TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task15_sort_by_due_is_not_chronological()
    {
        var sim = new Task15Todo();
        var cut = Todo(sim);
        Click(cut, "Sortuj po terminie");
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task23_delete_in_filter_removes_other_row()
    {
        var sim = new Task23Todo();
        var cut = Todo(sim);
        Click(cut, "Zaległe");
        var title = cut.Find("tbody tr td:nth-child(2)").TextContent;
        cut.Find("tbody button").Click();
        Assert.Contains(sim.Items, i => i.Title == title);
        Assert.True(Shown(sim));
    }

    // ---------- G. Podział rachunku ----------

    private IRenderedComponent<BillTask> Bill(BillSimulationBase sim, string amount, string persons, string code = "")
    {
        var cut = RenderComponent<BillTask>(p => p.Add(c => c.Simulation, sim));
        Input(cut, 0).Change(amount);
        Input(cut, 1).Change(persons);
        Input(cut, 2).Change(code);
        Click(cut, "Podziel rachunek");
        return cut;
    }

    [Fact]
    public void Task04_shares_sum_differs_from_total()
    {
        var sim = new Task04Bill();
        var cut = Bill(sim, "100", "3");
        Assert.Contains("99,99 zł", cut.Find("tfoot").TextContent);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task11_lower_case_code_reused()
    {
        var sim = new Task11Bill();
        var cut = Bill(sim, "200", "2", "TESTER10");
        Input(cut, 2).Change("tester10");
        Click(cut, "Podziel rachunek");
        Assert.Contains("Rabat TESTER10", cut.Markup);
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task21_dot_thousands_amount_misread()
    {
        var sim = new Task21Bill();
        var cut = Bill(sim, "1.000,50", "2");
        Assert.Contains("1,00 zł", cut.Find("dl.details").TextContent);
        Assert.True(Shown(sim));
    }

    // ---------- H. Zamówienie z dostawą ----------

    private IRenderedComponent<DeliveryTask> Delivery(DeliverySimulationBase sim) =>
        RenderComponent<DeliveryTask>(p => p.Add(c => c.Simulation, sim));

    private static IElement Field(IRenderedFragment cut, DeliveryField field) =>
        cut.Find("#" + DeliverySimulationBase.FieldId(field));

    [Fact]
    public void Task08_errors_have_no_text()
    {
        var sim = new Task08Delivery();
        var cut = Delivery(sim);
        Click(cut, "Zamów");
        Assert.Empty(cut.FindAll(".field-error"));
        Assert.Equal("true", Field(cut, DeliveryField.FirstName).GetAttribute("aria-invalid"));
        Assert.Null(Field(cut, DeliveryField.FirstName).GetAttribute("aria-describedby"));
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Correct_delivery_errors_are_described()
    {
        var sim = new Task24Delivery();
        var cut = Delivery(sim);
        Click(cut, "Zamów");
        var describedBy = Field(cut, DeliveryField.FirstName).GetAttribute("aria-describedby");
        Assert.Equal("Podaj imię.", cut.Find("#" + describedBy).TextContent);
        Assert.False(Shown(sim));
    }

    [Fact]
    public void Task17_last_name_label_targets_first_name()
    {
        var sim = new Task17Delivery();
        var cut = Delivery(sim);
        var label = cut.FindAll("label").First(l => l.TextContent.Trim() == "Nazwisko*");
        Assert.Equal(DeliverySimulationBase.FieldId(DeliveryField.FirstName), label.GetAttribute("for"));
        label.Click();
        Assert.True(Shown(sim));
    }

    [Fact]
    public void Task24_notes_cut_after_save()
    {
        var sim = new Task24Delivery();
        var cut = Delivery(sim);
        Field(cut, DeliveryField.FirstName).Change("Anna");
        Field(cut, DeliveryField.LastName).Change("Nowak");
        Field(cut, DeliveryField.Street).Change("Testowa 1");
        Field(cut, DeliveryField.PostalCode).Change("00-950");
        Field(cut, DeliveryField.City).Change("Warszawa");
        Field(cut, DeliveryField.Phone).Change("600 000 000");
        Field(cut, DeliveryField.Notes).Input(new string('a', 300));
        Assert.Contains("Pozostało znaków: 200", cut.Markup);
        Click(cut, "Zamów");
        Assert.Contains("Uwagi (255 znaków)", cut.Markup);
        Assert.True(Shown(sim));
    }
}
