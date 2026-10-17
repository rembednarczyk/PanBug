using PanBug.Domain.Model;

namespace PanBug.Domain.Defects;

/// <summary>
/// Deklaratywny opis defektu zadania na potrzeby punktacji: punkty, kategorie uznawane
/// za poprawne oraz kody akcji dowodzące, że gracz wywołał defekt. Opisy defektów
/// i kroki odtworzenia są w kluczu dla prowadzącego, poza aplikacją.
/// </summary>
public sealed record DefectDefinition(
    uint TaskNumber,
    PointValue Points,
    IReadOnlyList<DefectCategory> Categories,
    IReadOnlyList<uint> ReproductionMarkers)
{
    public int PointsValue => (int)Points;

    public bool IsReproduced(IEnumerable<uint> actions)
    {
        if (ReproductionMarkers.Count == 0)
            return false;
        var set = actions.ToHashSet();
        return ReproductionMarkers.Any(set.Contains);
    }

    public bool CategoriesMatch(IEnumerable<uint> reported)
    {
        var expected = Categories.Select(c => (uint)c).ToHashSet();
        return reported.Any(expected.Contains);
    }
}
