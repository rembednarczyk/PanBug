namespace PanBug.Domain.Model;

/// <summary>
/// Kategorie defektów, do których gracz przypisuje zgłoszenie. Lista łączy techniki
/// projektowania testów z sylabusa ISTQB (wartości brzegowe, klasy równoważności, reguły,
/// przejścia stanów) z obszarami jakości, w których defekty zdarzają się najczęściej.
/// </summary>
public enum DefectCategory
{
    WartosciBrzegowe = 1,
    KlasyRownowaznosci = 2,
    RegulyBiznesowe = 3,
    PrzejsciaStanow = 4,
    Obliczenia = 5,
    DatyICzas = 6,
    FormatyILokalizacja = 7,
    SpojnoscDanych = 8,
    InterfejsNiezgodnyZDanymi = 9,
    Bezpieczenstwo = 10,
    Dostepnosc = 11,
    Niezawodnosc = 12,
}

/// <summary>Pojedyncza kategoria prezentowana w oknie wyboru defektu i w pomocy.</summary>
public sealed record Answer(uint Number, string Content, string Hint);

public static class DefectCategories
{
    /// <summary>Pełna lista kategorii w kolejności wyświetlania.</summary>
    public static IReadOnlyList<Answer> All { get; } = new[]
    {
        new Answer(1, "Wartości brzegowe",
            "Błąd na granicy dozwolonego zakresu, np. wartość równa limitowi jest odrzucana."),
        new Answer(2, "Klasy równoważności (walidacja wejścia)",
            "Poprawne dane są odrzucane albo niepoprawne przyjmowane."),
        new Answer(3, "Reguły biznesowe",
            "Aplikacja liczy lub decyduje inaczej, niż mówi reguła z opisu."),
        new Answer(4, "Przejścia stanów",
            "Błąd pojawia się po określonej sekwencji akcji, np. zaznacz, odznacz, zaznacz."),
        new Answer(5, "Obliczenia i zaokrąglenia",
            "Wynik działania jest błędny, gubi grosze albo nie jest liczbą."),
        new Answer(6, "Daty i czas",
            "Błąd przy dniach tygodnia, przełomie roku, północy albo kolejności dat."),
        new Answer(7, "Formaty i lokalizacja",
            "Polskie formaty liczb, dat lub numerów są źle odczytywane albo pokazywane."),
        new Answer(8, "Spójność danych",
            "Zapisane dane różnią się od wprowadzonych albo zmiana nie trafia we wszystkie miejsca."),
        new Answer(9, "Interfejs niezgodny z danymi",
            "Ekran pokazuje co innego, niż aplikacja faktycznie zapisuje lub liczy."),
        new Answer(10, "Bezpieczeństwo i prywatność",
            "Aplikacja ujawnia informacje albo pozwala obejść zabezpieczenie."),
        new Answer(11, "Dostępność",
            "Funkcja jest niedostępna lub niezrozumiała dla osób korzystających z czytnika ekranu lub klawiatury."),
        new Answer(12, "Niezawodność",
            "Aplikacja przestaje działać poprawnie albo pokazuje wynik awarii."),
    };

    public static string NameOf(DefectCategory category) =>
        All[(int)category - 1].Content;
}
