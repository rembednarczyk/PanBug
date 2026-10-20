using PanBug.Domain.Defects;
using PanBug.Domain.Model;
using PanBug.Domain.Scoring;
using PanBug.Domain.Tasks;
using Xunit;

namespace PanBug.Domain.Tests;

public class ResultSummaryTests
{
    private static ReportTask Solved(uint n) => new(n)
    {
        IsCompleted = true,
        AnswerNumbers = new[] { (uint)DefectCatalog.For(n).Categories[0] },
        Actions = new uint[] { TaskBase.DefectMarker },
    };

    private static Report ReportOf(params ReportTask[] done)
    {
        var byNumber = done.ToDictionary(t => t.Number);
        var tasks = Enumerable.Range(1, TaskCatalog.TaskCount)
            .Select(n => byNumber.GetValueOrDefault((uint)n) ?? new ReportTask((uint)n))
            .ToList();
        return new Report(TimeSpan.FromSeconds(754), tasks);
    }

    private static ScoreResult Score(Report r) => new StandardScoringEngine().Score(r);

    [Fact]
    public void Profile_counts_each_category_from_the_catalog()
    {
        var profile = CategoryProfile.Build(Score(ReportOf()));

        // Każda z 12 kategorii ma zadanie, a zadanie z dwiema kategoriami liczy się w obu.
        Assert.Equal(DefectCategories.All.Count, profile.Count);
        Assert.Equal(DefectCatalog.All.Sum(d => d.Categories.Count), profile.Sum(e => e.Tasks));
        // Wartości brzegowe: zadania 1, 2, 7, 10.
        Assert.Equal(4, profile.Single(e => e.Category == DefectCategory.WartosciBrzegowe).Tasks);
        Assert.All(profile, e => Assert.Equal(0, e.Solved));
    }

    [Fact]
    public void Profile_counts_only_full_points_as_solved()
    {
        var wrongCategory = new ReportTask(4) // obliczenia
        {
            IsCompleted = true,
            AnswerNumbers = new uint[] { 1 },
            Actions = new uint[] { TaskBase.DefectMarker },
        };
        var profile = CategoryProfile.Build(Score(ReportOf(Solved(1), Solved(2), wrongCategory)));

        var boundary = profile.Single(e => e.Category == DefectCategory.WartosciBrzegowe);
        Assert.Equal(2, boundary.Solved);                 // 1 i 2
        Assert.Equal(0.5, boundary.Ratio);
        Assert.Equal(1, profile.Single(e => e.Category == DefectCategory.RegulyBiznesowe).Solved); // zadanie 1 ma też regułę
        Assert.Equal(0, profile.Single(e => e.Category == DefectCategory.Obliczenia).Solved);
    }

    [Fact]
    public void Weakest_prefers_lower_ratio_then_more_tasks_and_skips_perfect()
    {
        var profile = new[]
        {
            new CategoryProfileEntry(DefectCategory.Obliczenia, 5, 1),          // 0,2
            new CategoryProfileEntry(DefectCategory.Dostepnosc, 2, 0),          // 0, mało zadań
            new CategoryProfileEntry(DefectCategory.SpojnoscDanych, 5, 0),      // 0, więcej zadań
            new CategoryProfileEntry(DefectCategory.Bezpieczenstwo, 3, 3),      // komplet
        };

        var weakest = CategoryProfile.Weakest(profile, 5);

        Assert.Equal(new[] { DefectCategory.SpojnoscDanych, DefectCategory.Dostepnosc, DefectCategory.Obliczenia },
            weakest.Select(e => e.Category));
    }

    [Fact]
    public void Share_text_has_a_square_per_task_in_game_order()
    {
        var notReproduced = new ReportTask(3) { IsCompleted = true, AnswerNumbers = new uint[] { 3 } };
        var wrongCategory = new ReportTask(9)
        {
            IsCompleted = true,
            AnswerNumbers = new uint[] { 1 },
            Actions = new uint[] { TaskBase.DefectMarker },
        };
        var report = ReportOf(Solved(1), Solved(24), notReproduced, wrongCategory);

        var text = ShareText.Build(Score(report), report, "https://example.org/PanBug/");
        var lines = text.Split('\n');

        Assert.Equal("PanBug: 4/51 pkt, zadania 4/24, czas 00:12:34", lines[0]);
        Assert.Equal("🟩⬜🟥⬜⬜⬜⬜⬜", lines[1]);
        Assert.Equal("🟨⬜⬜⬜⬜⬜⬜⬜", lines[2]);
        Assert.Equal("⬜⬜⬜⬜⬜⬜⬜🟩", lines[3]);
        Assert.Equal("https://example.org/PanBug/", lines[4]);
        Assert.Equal(5, lines.Length);
    }

    [Fact]
    public void Share_text_reveals_no_category_names()
    {
        var report = ReportOf(Solved(1));
        var text = ShareText.Build(Score(report), report, "");

        Assert.DoesNotContain(DefectCategories.All, a => text.Contains(a.Content, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, text.Split('\n').Length); // bez pustego wiersza na adres
    }
}
