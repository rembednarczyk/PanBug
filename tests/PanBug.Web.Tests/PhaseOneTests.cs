using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanBug.Domain.Model;
using PanBug.Domain.Scoring;
using PanBug.Domain.Tasks;
using PanBug.Domain.Tasks.Cinema;
using PanBug.Web.Pages;
using PanBug.Web.Services;
using PanBug.Web.Shared;
using Xunit;

namespace PanBug.Web.Tests;

/// <summary>Limit kategorii w zgłoszeniu, kopia wyników, profil kategorii i wynik do udostępnienia.</summary>
public class PhaseOneTests : WebTestBase
{
    private static GameRecord Game(int points, string finishedAt, long durationMs = 60_000) =>
        new(points, 51, 24, durationMs, DateTime.Parse(finishedAt).ToUniversalTime());

    // ---------- Limit kategorii ----------

    [Fact]
    public void Dialog_blocks_a_third_category()
    {
        var cut = RenderComponent<DefectDialog>(p => p.Add(d => d.TaskNumber, 1u));
        IReadOnlyList<AngleSharp.Dom.IElement> Boxes() => cut.FindAll("input[type=checkbox]");

        Boxes()[0].Change(true);
        Boxes()[3].Change(true);

        Assert.Equal(2, Boxes().Count(b => b.HasAttribute("checked")));
        Assert.Equal(DefectCategories.All.Count - 2, Boxes().Count(b => b.HasAttribute("disabled")));
        Assert.Contains("2 z 2", cut.Find(".category-limit").TextContent);

        // Odznaczenie zwalnia miejsce.
        Boxes()[0].Change(false);
        Assert.Empty(Boxes().Where(b => b.HasAttribute("disabled")));
    }

    [Fact]
    public void Dialog_reports_at_most_two_categories_even_if_a_disabled_box_fires()
    {
        IReadOnlyList<uint>? reported = null;
        var cut = RenderComponent<DefectDialog>(p => p
            .Add(d => d.TaskNumber, 1u)
            .Add(d => d.OnConfirm, (IReadOnlyList<uint> r) => reported = r));
        var boxes = cut.FindAll("input[type=checkbox]");
        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("input[type=checkbox]")[1].Change(true);
        // Zdarzenie z wyłączonego pola (np. wysłane spoza UI) nie może dodać trzeciej kategorii.
        cut.FindAll("input[type=checkbox]")[2].Change(true);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "ZATWIERDŹ").Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Tak").Click();

        Assert.Equal(new uint[] { 1, 2 }, reported);
        Assert.Equal(DefectCategories.All.Count, boxes.Count);
    }

    // ---------- Kopia wyników ----------

    [Fact]
    public void Backup_round_trips_and_rejects_foreign_files()
    {
        var history = new[] { Game(10, "2026-10-01T10:00:00Z"), Game(7, "2026-09-01T10:00:00Z") };
        var json = ResultsBackup.Serialize(history[0], history, DateTime.UtcNow);

        var parsed = ResultsBackup.Parse(json);

        Assert.NotNull(parsed);
        Assert.Equal(history, parsed!.History);
        Assert.Equal(history[0], parsed.Best);

        Assert.Null(ResultsBackup.Parse("{ to nie json"));
        Assert.Null(ResultsBackup.Parse("[]"));
        Assert.Null(ResultsBackup.Parse(json.Replace(ResultsBackup.Format, "inna-gra")));
        Assert.Null(ResultsBackup.Parse(json.Replace("\"Version\": 1", "\"Version\": 2")));
    }

    [Fact]
    public void Backup_drops_implausible_entries()
    {
        const string json = """
            {"Format":"panbug-results","Version":1,"ExportedAtUtc":"2026-10-01T00:00:00Z",
             "Best":{"Points":99,"MaxPoints":51,"CompletedTasks":24,"DurationMs":1000,"FinishedAtUtc":"2026-10-01T00:00:00Z"},
             "History":[null,
               {"Points":-1,"MaxPoints":51,"CompletedTasks":3,"DurationMs":1000,"FinishedAtUtc":"2026-10-01T00:00:00Z"},
               {"Points":5,"MaxPoints":51,"CompletedTasks":3,"DurationMs":1000,"FinishedAtUtc":"2026-10-01T00:00:00Z"}]}
            """;

        var parsed = ResultsBackup.Parse(json)!;

        Assert.Null(parsed.Best);                         // 99 > 51: uszkodzony
        Assert.Equal(5, Assert.Single(parsed.History).Points);
    }

    [Fact]
    public void Merge_adds_new_games_once_and_keeps_the_best_beyond_the_limit()
    {
        var shared = Game(5, "2026-10-02T10:00:00Z");
        var current = new[] { Game(8, "2026-10-03T10:00:00Z"), shared };
        var oldRecord = Game(40, "2025-01-01T10:00:00Z");
        var imported = new ResultsBackupFile(ResultsBackup.Format, 1, DateTime.UtcNow, null,
            new[] { shared, Game(3, "2026-10-01T10:00:00Z"), oldRecord });

        var merged = ResultsBackup.Merge(current[0], current, imported, limit: 3);

        Assert.Equal(2, merged.Added);                    // wspólna gra nie liczy się drugi raz
        Assert.Equal(new[] { 8, 5, 3 }, merged.History.Select(g => g.Points)); // od najnowszej, limit 3
        Assert.Equal(40, merged.Best!.Points);            // rekord z historii pliku, choć wypadł poza limit
    }

    [Fact]
    public async Task Local_store_import_writes_merged_history()
    {
        var js = new FakeJsRuntime();
        var store = new LocalStorageResultStore(js);
        await store.SaveAsync(Game(6, "2026-10-03T10:00:00Z"));

        var added = await store.ImportAsync(ResultsBackup.Parse(
            ResultsBackup.Serialize(null, new[] { Game(9, "2026-10-01T10:00:00Z") }, DateTime.UtcNow))!);

        Assert.Equal(1, added);
        Assert.Equal(new[] { 6, 9 }, (await store.GetHistoryAsync()).Select(g => g.Points));
        Assert.Equal(9, (await store.GetBestAsync())!.Points);

        js.Writable = false;
        Assert.Null(await store.ImportAsync(ResultsBackup.Parse(
            ResultsBackup.Serialize(null, new[] { Game(2, "2026-09-01T10:00:00Z") }, DateTime.UtcNow))!));
    }

    [Fact]
    public void Home_exports_results_to_a_file()
    {
        Results.History.Add(Game(7, "2026-10-01T10:00:00Z"));
        Results.Best = Results.History[0];
        var cut = RenderComponent<Home>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Eksportuj wyniki")).Click();

        var call = JSInterop.VerifyInvoke("panbug.download");
        Assert.StartsWith("panbug-wyniki-", (string)call.Arguments[0]!);
        var parsed = ResultsBackup.Parse((string)call.Arguments[1]!);
        Assert.Equal(7, Assert.Single(parsed!.History).Points);
    }

    [Fact]
    public void Home_without_history_offers_only_import()
    {
        var cut = RenderComponent<Home>();

        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("Eksportuj"));
        Assert.Contains("Importuj wyniki", cut.Find("label.file-button").TextContent);
    }

    [Fact]
    public void Home_imports_a_backup_file()
    {
        var cut = RenderComponent<Home>();
        var json = ResultsBackup.Serialize(null, new[] { Game(12, "2026-10-01T10:00:00Z") }, DateTime.UtcNow);

        cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>()
            .UploadFiles(InputFileContent.CreateFromText(json, "panbug-wyniki.json"));

        cut.WaitForAssertion(() => Assert.Contains("Dodano 1 grę", cut.Markup));
        Assert.Equal(12, Results.Best!.Points);
        Assert.Contains("12", cut.Find(".best-points").TextContent);
    }

    [Fact]
    public void Home_rejects_a_foreign_file()
    {
        var cut = RenderComponent<Home>();

        cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>()
            .UploadFiles(InputFileContent.CreateFromText("{\"a\":1}", "cos.json"));

        cut.WaitForAssertion(() => Assert.Contains("To nie jest plik z wynikami PanBuga", cut.Markup));
        Assert.Empty(Results.History);
    }

    // ---------- Ekran wyniku ----------

    private async Task FinishWithTaskOneSolved()
    {
        // Zadanie 1: bilet dla 12-latka bez ulgi to defekt.
        var manager = Session.StartGame();
        var cinema = (CinemaSimulationBase)manager.Current.Simulation;
        cinema.SelectShowing(cinema.ShowingsFor(cinema.Today)[0]);
        cinema.ToggleSeat(new Seat(1, 1));
        cinema.Tickets[0].Age = "12";
        cinema.Summarize();
        Assert.True(cinema.DefectShown);
        manager.CompleteCurrentTask(new uint[] { 1 });
        await Session.FinishAsync();
    }

    [Fact]
    public async Task Result_shows_category_profile()
    {
        await FinishWithTaskOneSolved();
        var cut = RenderComponent<Result>();

        var rows = cut.FindAll(".profile-list li");
        Assert.Equal(DefectCategories.All.Count, rows.Count);
        // Zadanie 1: wartości brzegowe i reguły biznesowe, po jednym zaliczonym.
        Assert.Contains("1/4", rows.Single(r => r.TextContent.Contains("Wartości brzegowe")).TextContent);
        Assert.Contains("Do poćwiczenia", cut.Find(".profile-weak").TextContent);
    }

    [Fact]
    public async Task Result_copies_share_text_only_on_request()
    {
        await FinishWithTaskOneSolved();
        JSInterop.Setup<bool>("panbug.copy", _ => true).SetResult(true);
        var cut = RenderComponent<Result>();
        Assert.Empty(cut.FindAll(".share"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Kopiuj wynik")).Click();

        var text = (string)JSInterop.VerifyInvoke("panbug.copy").Arguments[0]!;
        Assert.StartsWith("PanBug: 1/51 pkt", text);
        Assert.Contains(ShareText.Solved, text);
        Assert.Contains("Skopiowano", cut.Find(".share").TextContent);
        Assert.Equal(text, cut.Find(".share textarea").GetAttribute("value"));
    }
}
