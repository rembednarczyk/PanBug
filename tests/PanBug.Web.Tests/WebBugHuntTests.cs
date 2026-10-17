using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using PanBug.Web.Pages;
using Xunit;

namespace PanBug.Web.Tests;

/// <summary>Regresje z bughuntu warstwy web: okna dialogowe, rekord, nawigacja.</summary>
public class WebBugHuntTests : WebTestBase
{
    [Fact]
    public void Open_report_dialog_disables_the_board_and_escape_cancels()
    {
        Session.StartGame();
        var cut = RenderComponent<Game>();
        Assert.False(cut.Find(".game").HasAttribute("inert"));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "ZGŁOŚ DEFEKT").Click();

        // Plansza pod oknem jest nieaktywna, więc Tab nie przełączy zadania pod zgłoszeniem,
        // a fokus i Escape obsługuje panbug.modal (app.js).
        Assert.True(cut.Find(".game").HasAttribute("inert"));
        JSInterop.VerifyInvoke("panbug.modal.open");

        // Escape klika przycisk z data-escape: w ostrzeżeniu "Nie" (powrót), w wyborze kategorii "Anuluj".
        cut.Find("input[type=checkbox]").Change(true);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "ZATWIERDŹ").Click();
        Assert.Equal("Nie", cut.Find("[role=dialog] [data-escape]").TextContent.Trim());
        cut.Find("[role=dialog] [data-escape]").Click();
        Assert.Equal("Anuluj", cut.Find("[role=dialog] [data-escape]").TextContent.Trim());
        cut.Find("[role=dialog] [data-escape]").Click();

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.False(cut.Find(".game").HasAttribute("inert"));
        JSInterop.VerifyInvoke("panbug.modal.close");
    }

    [Fact]
    public void End_game_dialog_escape_means_no()
    {
        Session.StartGame();
        var cut = RenderComponent<Game>();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "ZAKOŃCZ").Click();

        Assert.Equal("Nie", cut.Find("[role=dialog] [data-escape]").TextContent.Trim());
        cut.Find("[role=dialog] [data-escape]").Click();

        Assert.Empty(cut.FindAll("[role=dialog]"));
        Assert.False(Session.HasResult);
    }

    [Fact]
    public async Task Zero_point_game_is_not_a_new_record()
    {
        Session.StartGame();
        await Session.FinishAsync();

        Assert.Equal(0, Session.LastScore!.Points);
        Assert.False(Session.IsNewRecord);
    }

    [Fact]
    public void Result_without_a_game_replaces_the_history_entry()
    {
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("wynik");

        RenderComponent<Result>();

        // replace: inaczej "Wstecz" wraca na /wynik i przekierowuje w kółko.
        Assert.True(nav.History.First().Options.ReplaceHistoryEntry);
        Assert.Equal(nav.BaseUri, nav.Uri);
    }
}
