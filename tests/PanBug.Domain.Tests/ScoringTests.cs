using PanBug.Domain.Defects;
using PanBug.Domain.Model;
using PanBug.Domain.Scoring;
using PanBug.Domain.Tasks;
using Xunit;

namespace PanBug.Domain.Tests;

public class ScoringTests
{
    private static Report SampleReport(params ReportTask[] tasks) =>
        new(TimeSpan.FromSeconds(90), tasks);

    [Fact]
    public void Scoring_awards_points_when_reproduced_and_category_matches()
    {
        // Zadanie 1: kategorie 1 i 3, wystarczy jedna z nich.
        var report = SampleReport(new ReportTask(1)
        {
            IsCompleted = true,
            AnswerNumbers = new uint[] { 3 },
            Actions = new uint[] { TaskBase.DefectMarker },
        });

        var result = new StandardScoringEngine().Score(report);

        Assert.Equal(1, result.Points);
        Assert.Equal(DefectCatalog.MaxPoints, result.MaxPoints);
        Assert.True(result.Breakdown.Single().Reproduced);
    }

    [Fact]
    public void Scoring_zero_when_category_wrong()
    {
        var report = SampleReport(new ReportTask(2)
        {
            IsCompleted = true,
            AnswerNumbers = new uint[] { 3 }, // zadanie 2 to tylko kategoria 1
            Actions = new uint[] { TaskBase.DefectMarker },
        });

        Assert.Equal(0, new StandardScoringEngine().Score(report).Points);
    }

    [Fact]
    public void Scoring_zero_when_defect_not_reproduced()
    {
        var report = SampleReport(new ReportTask(2)
        {
            IsCompleted = true,
            AnswerNumbers = new uint[] { 1 }, // trafna kategoria, ale defekt nie odtworzony
            Actions = Array.Empty<uint>(),
        });

        Assert.Equal(0, new StandardScoringEngine().Score(report).Points);
    }

    [Fact]
    public void Selecting_every_category_no_longer_scores()
    {
        // Obejście sprzed limitu: zaznaczenie wszystkich 12 kategorii zawsze zawierało poprawną.
        var report = SampleReport(new ReportTask(23)
        {
            IsCompleted = true,
            AnswerNumbers = DefectCategories.All.Select(a => a.Number).ToArray(),
            Actions = new uint[] { TaskBase.DefectMarker },
        });

        var task = new StandardScoringEngine().Score(report).Breakdown.Single();

        Assert.Equal(0, task.AwardedPoints);
        Assert.True(task.Reproduced);
        Assert.False(task.CategoryMatched);
    }

    [Theory]
    [InlineData(new uint[] { 4, 8 }, 4)]      // obie poprawne
    [InlineData(new uint[] { 4, 1 }, 4)]      // jedna poprawna, jedna nie: limit, nie kara
    [InlineData(new uint[] { 4, 8, 1 }, 0)]   // trzy to ponad limit, nawet z dwiema poprawnymi
    [InlineData(new uint[] { 4, 4, 4 }, 4)]   // powtórzenia liczą się raz
    public void Report_may_name_at_most_two_categories(uint[] answers, int expected)
    {
        // Zadanie 23 (4 pkt): przejścia stanów (4) i spójność danych (8).
        var report = SampleReport(new ReportTask(23)
        {
            IsCompleted = true,
            AnswerNumbers = answers,
            Actions = new uint[] { TaskBase.DefectMarker },
        });

        Assert.Equal(expected, new StandardScoringEngine().Score(report).Points);
    }

    [Fact]
    public void Every_defect_fits_within_the_category_limit()
    {
        // Gdyby defekt miał więcej poprawnych kategorii niż limit, nie dałoby się wskazać wszystkich.
        Assert.All(DefectCatalog.All, d => Assert.InRange(d.Categories.Count, 1, DefectCategories.MaxPerReport));
    }

    [Fact]
    public void Scoring_partial_credit_option()
    {
        var report = SampleReport(new ReportTask(23) // 4 punkty
        {
            IsCompleted = true,
            AnswerNumbers = new uint[] { 1 }, // zła kategoria
            Actions = new uint[] { TaskBase.DefectMarker },
        });

        var engine = new StandardScoringEngine(new ScoringOptions { PartialCreditForReproductionOnly = true });
        Assert.Equal(2, engine.Score(report).Points); // połowa z 4
    }

    [Fact]
    public void Tasks_of_one_app_are_spread_and_never_adjacent()
    {
        // Zadania jednej aplikacji dzielą opis wymagań, więc nie mogą stać obok siebie
        // a gra zaczyna się od kina.
        var apps = TaskCatalog.CreateAll().Select(t => t.GetType().BaseType!).ToList();

        Assert.Equal(typeof(PanBug.Domain.Tasks.Cinema.CinemaSimulationBase), apps[0]);
        Assert.All(apps.GroupBy(a => a), g => Assert.Equal(3, g.Count()));
        for (var i = 1; i < apps.Count; i++)
            Assert.NotEqual(apps[i - 1], apps[i]);
        foreach (var group in apps.Select((a, i) => (a, i)).GroupBy(x => x.a))
        {
            var positions = group.Select(x => x.i).ToList();
            Assert.True(positions.Zip(positions.Skip(1)).All(p => p.Second - p.First >= 4),
                $"{group.Key.Name}: zadania {string.Join(", ", positions.Select(i => i + 1))} są za blisko siebie.");
        }
    }

    [Fact]
    public void Catalog_covers_all_24_tasks_with_51_points()
    {
        Assert.Equal(24, DefectCatalog.All.Count);
        for (uint n = 1; n <= 24; n++)
            Assert.True(DefectCatalog.TryGet(n, out _));
        Assert.Equal(51, DefectCatalog.MaxPoints);
    }

    [Fact]
    public void Every_category_is_used_and_numbered_like_the_list()
    {
        Assert.Equal(12, DefectCategories.All.Count);
        for (int i = 0; i < DefectCategories.All.Count; i++)
        {
            Assert.Equal((uint)(i + 1), DefectCategories.All[i].Number);
            Assert.False(string.IsNullOrWhiteSpace(DefectCategories.All[i].Hint));
        }
        var used = DefectCatalog.All.SelectMany(d => d.Categories).Select(c => (uint)c).ToHashSet();
        Assert.Equal(DefectCategories.All.Select(a => a.Number).ToHashSet(), used);
    }

    [Fact]
    public void Task_catalog_matches_defect_catalog()
    {
        var tasks = TaskCatalog.CreateAll();
        Assert.Equal(24, tasks.Count);
        foreach (var task in tasks)
        {
            Assert.Equal(DefectCatalog.For(task.Number).Points, task.Points);
            Assert.False(task.DefectShown());
            Assert.Equal(12, task.Categories.Count);
        }
    }
}

internal static class SimulationExtensions
{
    public static bool DefectShown(this ITaskSimulation simulation) =>
        simulation.Actions.Contains(TaskBase.DefectMarker);
}
