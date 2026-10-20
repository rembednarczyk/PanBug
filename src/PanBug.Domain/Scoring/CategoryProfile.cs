using PanBug.Domain.Defects;
using PanBug.Domain.Model;

namespace PanBug.Domain.Scoring;

/// <summary>Wynik gracza w jednej kategorii: ile zadań jej dotyczy i ile z nich zaliczył w pełni.</summary>
public sealed record CategoryProfileEntry(DefectCategory Category, int Tasks, int Solved)
{
    public string Name => DefectCategories.NameOf(Category);

    /// <summary>Udział zaliczonych zadań kategorii, 0-1.</summary>
    public double Ratio => Tasks == 0 ? 0 : (double)Solved / Tasks;
}

/// <summary>
/// Profil kategorii po grze: wynik per kategoria defektu, żeby gracz widział, które rodzaje
/// defektów znajduje, a które mu umykają. Zadanie z dwiema kategoriami liczy się w obu.
/// Zaliczone znaczy: pełne punkty (defekt odtworzony i kategoria trafiona).
/// Na kategorię przypada od 1 do 7 zadań, więc profil to wskazówka, nie pomiar.
/// </summary>
public static class CategoryProfile
{
    public static IReadOnlyList<CategoryProfileEntry> Build(ScoreResult score)
    {
        var solved = score.Breakdown
            .Where(t => t.MaxPoints > 0 && t.AwardedPoints == t.MaxPoints)
            .Select(t => t.TaskNumber)
            .ToHashSet();

        return DefectCatalog.All
            .SelectMany(d => d.Categories.Distinct().Select(c => (Category: c, d.TaskNumber)))
            .GroupBy(x => x.Category)
            .OrderBy(g => g.Key)
            .Select(g => new CategoryProfileEntry(g.Key, g.Count(), g.Count(x => solved.Contains(x.TaskNumber))))
            .ToList();
    }

    /// <summary>
    /// Kategorie do poćwiczenia: najniższy udział zaliczonych zadań, przy remisie ta z większą
    /// liczbą zadań (więcej okazji, większa pewność). Kategorie zaliczone w całości są pomijane.
    /// </summary>
    public static IReadOnlyList<CategoryProfileEntry> Weakest(IReadOnlyList<CategoryProfileEntry> profile, int count = 3) =>
        profile
            .Where(e => e.Solved < e.Tasks)
            .OrderBy(e => e.Ratio)
            .ThenByDescending(e => e.Tasks)
            .ThenBy(e => e.Category)
            .Take(count)
            .ToList();
}
