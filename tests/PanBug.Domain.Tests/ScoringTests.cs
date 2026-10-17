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
            AnswerNumbers = new uint[] { 1 }, // zadanie 2 to kategorie 4 i 5
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
            AnswerNumbers = new uint[] { 4 },
            Actions = Array.Empty<uint>(),
        });

        Assert.Equal(0, new StandardScoringEngine().Score(report).Points);
    }

    [Fact]
    public void Scoring_partial_credit_option()
    {
        var report = SampleReport(new ReportTask(18) // 4 punkty
        {
            IsCompleted = true,
            AnswerNumbers = new uint[] { 1 }, // zła kategoria
            Actions = new uint[] { TaskBase.DefectMarker },
        });

        var engine = new StandardScoringEngine(new ScoringOptions { PartialCreditForReproductionOnly = true });
        Assert.Equal(2, engine.Score(report).Points); // połowa z 4
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
