using PanBug.Domain.Model;
using PanBug.Domain.Tasks;
using static PanBug.Domain.Model.DefectCategory;

namespace PanBug.Domain.Defects;

/// <summary>
/// Punktacja wszystkich 24 zadań: kategorie uznawane za poprawne i liczba punktów.
/// Każde zadanie potwierdza odtworzenie defektu tym samym kodem
/// (<see cref="TaskBase.DefectMarker"/>), który symulacja zapisuje dopiero wtedy,
/// gdy defekt jest widoczny na ekranie.
/// Lista jest pogrupowana według aplikacji. Numery zadań jednej aplikacji są rozrzucone
/// po grze (zob. <see cref="TaskCatalog"/>).
/// </summary>
public static class DefectCatalog
{
    private static DefectDefinition D(uint n, PointValue p, params DefectCategory[] cats) =>
        new(n, p, cats, new[] { TaskBase.DefectMarker });

    private static readonly IReadOnlyDictionary<uint, DefectDefinition> Map =
        new[]
        {
            // A. Kino
            D(1, PointValue.One, WartosciBrzegowe, RegulyBiznesowe),
            D(9, PointValue.Two, PrzejsciaStanow, Obliczenia),
            D(18, PointValue.Three, DatyICzas, InterfejsNiezgodnyZDanymi),
            // B. Automat paczkowy
            D(2, PointValue.One, WartosciBrzegowe),
            D(6, PointValue.Two, KlasyRownowaznosci, SpojnoscDanych),
            D(13, PointValue.One, FormatyILokalizacja, InterfejsNiezgodnyZDanymi),
            // C. Kalkulator raty kredytu
            D(7, PointValue.Two, WartosciBrzegowe, Niezawodnosc),
            D(14, PointValue.Three, Obliczenia),
            D(20, PointValue.Two, InterfejsNiezgodnyZDanymi),
            // D. Planer urlopów
            D(3, PointValue.One, RegulyBiznesowe, DatyICzas),
            D(16, PointValue.Three, RegulyBiznesowe, DatyICzas),
            D(22, PointValue.Two, PrzejsciaStanow, SpojnoscDanych),
            // E. Logowanie i reset hasła
            D(5, PointValue.Two, Bezpieczenstwo),
            D(12, PointValue.Three, Bezpieczenstwo, KlasyRownowaznosci),
            D(19, PointValue.Two, Bezpieczenstwo, PrzejsciaStanow),
            // F. Lista zadań z terminami
            D(10, PointValue.Two, WartosciBrzegowe, DatyICzas),
            D(15, PointValue.Two, DatyICzas, FormatyILokalizacja),
            D(23, PointValue.Four, PrzejsciaStanow, SpojnoscDanych),
            // G. Podział rachunku
            D(4, PointValue.One, Obliczenia),
            D(11, PointValue.Two, RegulyBiznesowe, KlasyRownowaznosci),
            D(21, PointValue.Three, FormatyILokalizacja, Obliczenia),
            // H. Zamówienie z dostawą
            D(8, PointValue.Two, Dostepnosc),
            D(17, PointValue.Two, Dostepnosc, InterfejsNiezgodnyZDanymi),
            D(24, PointValue.Three, SpojnoscDanych, InterfejsNiezgodnyZDanymi),
        }.ToDictionary(d => d.TaskNumber);

    public static IReadOnlyCollection<DefectDefinition> All => (IReadOnlyCollection<DefectDefinition>)Map.Values;

    public static DefectDefinition For(uint taskNumber) => Map[taskNumber];

    public static bool TryGet(uint taskNumber, out DefectDefinition definition) =>
        Map.TryGetValue(taskNumber, out definition!);

    public static int MaxPoints => Map.Values.Sum(d => d.PointsValue);
}
