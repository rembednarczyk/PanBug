using AngleSharp.Dom;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PanBug.Domain.Tasks;
using PanBug.Web.Pages;
using PanBug.Web.Services;
using PanBug.Web.Tasks;
using Xunit;

namespace PanBug.Web.Tests;

public class WebUiTests : WebTestBase
{
    private static IElement ButtonWithText(IRenderedFragment cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    [Fact]
    public void Every_task_component_renders()
    {
        foreach (var simulation in TaskCatalog.CreateAll())
        {
            simulation.Reset();
            var component = TaskComponentRegistry.ComponentFor(simulation);
            var cut = Render(builder =>
            {
                builder.OpenComponent(0, component);
                builder.AddAttribute(1, "Simulation", simulation);
                builder.CloseComponent();
            });
            Assert.False(string.IsNullOrWhiteSpace(cut.Markup), $"Pusty widok zadania {simulation.Number}");
        }
    }

    [Fact]
    public void Player_reproduces_defect_reports_it_and_gets_points()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        Session.StartGame();
        var cut = RenderComponent<Game>();

        Assert.Equal(24, cut.FindAll("button.tile").Count);
        Assert.Contains("Zadanie #1", cut.Find("h1").TextContent);

        // Zadanie 1: bilet dla 12-latka bez ulgi to defekt.
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Wybierz miejsca").Click();
        cut.Find("button[aria-label='Rząd 1, miejsce 1']").Click();
        cut.Find("input.num").Change("12");
        ButtonWithText(cut, "Podsumowanie").Click();
        Assert.Contains("normalny", cut.Find("table.grid tbody").TextContent);

        ButtonWithText(cut, "ZGŁOŚ DEFEKT").Click();
        cut.FindAll("label.check").First(l => l.TextContent.Contains("Wartości brzegowe"))
            .QuerySelector("input")!.Change(true);
        ButtonWithText(cut, "ZATWIERDŹ").Click();
        ButtonWithText(cut, "Tak").Click();

        Assert.Contains("Zadanie #2", cut.Find("h1").TextContent);
        Assert.Contains("1/24", cut.Find(".progress-text").TextContent);

        ButtonWithText(cut, "ZAKOŃCZ").Click();
        ButtonWithText(cut, "Tak").Click();

        cut.WaitForAssertion(() => Assert.EndsWith("/wynik", nav.Uri));
        Assert.Equal(1, Session.LastScore!.Points);
        Assert.True(Session.IsNewRecord);
        Assert.Equal(1, Results.Best!.Points);
        Assert.Single(Results.History);

        // Ekran wyniku: duży wynik jest jeden (klasa .points nie może trafić na kafelki statystyk).
        var result = RenderComponent<Result>();
        Assert.Equal("1", Assert.Single(result.FindAll(".points")).TextContent.Trim());
    }

    [Fact]
    public void Task_shows_a_collapsed_mission_charter_of_its_app()
    {
        var manager = Session.StartGame();
        var cut = RenderComponent<Game>();

        var charter = cut.Find("details.charter");
        Assert.False(charter.HasAttribute("open")); // domyślnie zwinięta
        var expected = manager.Current.Simulation.Charter;
        Assert.Contains(expected.Goal, charter.TextContent);
        Assert.Equal(expected.Ideas.Count, cut.FindAll(".charter-ideas li").Count);
    }

    [Fact]
    public void Game_without_an_active_game_returns_to_the_welcome_screen()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("gra");

        var cut = RenderComponent<Game>();

        Assert.Equal(nav.BaseUri, nav.Uri);
        Assert.Null(Session.Manager);
        Assert.Empty(cut.FindAll("button.tile"));
    }

    [Fact]
    public void Defect_dialog_requires_a_category()
    {
        Session.StartGame();
        var cut = RenderComponent<Game>();

        ButtonWithText(cut, "ZGŁOŚ DEFEKT").Click();
        ButtonWithText(cut, "ZATWIERDŹ").Click();

        Assert.Contains("Musisz wybrać przynajmniej jedną kategorię.", cut.Markup);
    }

    [Fact]
    public void Home_shows_best_result_and_history()
    {
        Results.History.Add(new GameRecord(7, 44, 10, 600_000, DateTime.UtcNow));
        Results.Best = Results.History[0];

        var cut = RenderComponent<Home>();

        Assert.Contains("7", cut.Find(".best-points").TextContent);
        Assert.Single(cut.FindAll(".scores tbody tr"));
        Assert.Contains("ROZPOCZNIJ", cut.Markup);
    }

    [Fact]
    public void Home_offers_to_continue_active_game()
    {
        Session.StartGame();
        var cut = RenderComponent<Home>();
        Assert.Contains("KONTYNUUJ GRĘ", cut.Markup);
    }

    [Fact]
    public void Result_redirects_home_without_finished_game()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        RenderComponent<Result>();
        Assert.Equal(nav.BaseUri, nav.Uri);
    }
}
