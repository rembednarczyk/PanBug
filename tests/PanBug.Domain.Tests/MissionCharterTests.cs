using PanBug.Domain.Defects;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks;
using Xunit;

namespace PanBug.Domain.Tests;

public class MissionCharterTests
{
    private static IEnumerable<IGrouping<Type, ITaskSimulation>> Apps() =>
        TaskCatalog.CreateAll().GroupBy(t => t.GetType().BaseType!);

    [Fact]
    public void Every_app_has_one_charter_shared_by_its_three_tasks()
    {
        // Ta sama karta dla trzech zadań: karta nie może zdradzać, czym zadania się różnią.
        Assert.Equal(8, Apps().Count());
        Assert.All(Apps(), app =>
        {
            var charters = app.Select(t => t.Charter).Distinct().ToList();
            Assert.Single(charters);
            Assert.False(string.IsNullOrWhiteSpace(charters[0].Goal));
            Assert.InRange(charters[0].Ideas.Count, 3, 5);
            Assert.All(charters[0].Ideas, i =>
            {
                Assert.False(string.IsNullOrWhiteSpace(i.Heuristic));
                Assert.False(string.IsNullOrWhiteSpace(i.Question));
            });
        });
        Assert.Equal(8, Apps().Select(a => a.First().Charter).Distinct().Count());
    }

    [Fact]
    public void Every_charter_points_beyond_the_apps_defect_categories()
    {
        // Gdyby pomysły pokrywały się dokładnie z kategoriami defektów aplikacji, karta byłaby
        // ściągą. Co najmniej jeden pomysł musi dotyczyć obszaru bez defektu w tej aplikacji.
        Assert.All(Apps(), app =>
        {
            var defectAreas = app.SelectMany(t => DefectCatalog.For(t.Number).Categories).ToHashSet();
            var charter = app.First().Charter;
            Assert.True(charter.Ideas.Any(i => !defectAreas.Contains(i.Area)),
                $"{app.Key.Name}: karta nie ma pomysłu spoza kategorii defektów aplikacji.");
        });
    }

    [Fact]
    public void Charters_do_not_name_defect_categories()
    {
        // Karta podsuwa sposób testowania, nie nazwę kategorii z okna zgłoszenia.
        var texts = Apps().Select(a => a.First().Charter)
            .SelectMany(c => c.Ideas.SelectMany(i => new[] { i.Heuristic, i.Question }).Append(c.Goal));
        Assert.All(texts, text =>
        {
            Assert.DoesNotContain(DefectCategories.All, a => text.Contains(a.Content, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("—", text);
        });
    }
}
