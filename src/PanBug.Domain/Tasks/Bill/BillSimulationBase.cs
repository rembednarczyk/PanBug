using PanBug.Domain.Common;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Bill;

public sealed record BillResult(decimal Amount, string? Code, int DiscountPercent, decimal Discount, IReadOnlyList<decimal> Shares)
{
    public decimal ToPay => Amount - Discount;
    public decimal SharesTotal => Shares.Sum();
}

/// <summary>Podział rachunku w restauracji z kodem rabatowym.</summary>
public abstract class BillSimulationBase : TaskBase
{
    public const decimal MaxAmount = 10_000m;
    public const int MaxPersons = 20;

    public const string Spec =
        "Podział rachunku w restauracji.\n\n" +
        "Dane testowe: kody rabatowe TESTER10 (10%) i OBIAD15 (15%).\n\n" +
        "Wymagania:\n" +
        "• Kwotę rachunku (od 0,01 do 10 000 zł) można wpisać z separatorem tysięcy, spacją albo kropką, i groszami po przecinku, np. 1 250,50 albo 1.250,50.\n" +
        "• Rachunek dzieli się na od 1 do 20 osób. Suma kwot wszystkich osób jest równa kwocie do zapłaty, a groszową resztę płacą pierwsze osoby.\n" +
        "• Każdy kod rabatowy można wykorzystać tylko raz. Wielkość liter w kodzie nie ma znaczenia.";

    /// <summary>Karta misji wspólna dla trzech zadań aplikacji.</summary>
    public static readonly MissionCharter AppCharter = new(
        "Sprawdź, czy podział rachunku daje kwoty, które razem pokrywają cały rachunek.",
        new TestIdea[]
        {
            new("Formaty", "Wpisz kwotę na każdy dozwolony sposób: ze spacją, z kropką jako separatorem tysięcy, z groszami.", DefectCategory.FormatyILokalizacja),
            new("Grosze", "Podziel kwotę, która nie dzieli się równo, i zsumuj kwoty wszystkich osób.", DefectCategory.Obliczenia),
            new("Granice", "Podziel rachunek na 1 i na 20 osób, potem spróbuj 0 i 21.", DefectCategory.WartosciBrzegowe),
            new("Kody", "Użyj kodu rabatowego małymi literami, potem spróbuj użyć go drugi raz.", DefectCategory.RegulyBiznesowe),
        });

    public sealed override MissionCharter Charter => AppCharter;

    private static readonly Dictionary<string, int> Codes = new()
    {
        ["TESTER10"] = 10,
        ["OBIAD15"] = 15,
    };

    private readonly HashSet<string> _usedCodes = new();

    protected BillSimulationBase(uint number, PointValue points) : base(number, points, Spec) { }

    public BillResult? Result { get; private set; }

    public OperationResult Calculate(string? amountText, string? personsText, string? codeText)
    {
        Result = null;
        if (!ReadAmount(amountText, out var amount) || amount < 0.01m || amount > MaxAmount)
            return OperationResult.Fail("Kwota rachunku musi mieścić się w zakresie od 0,01 do 10 000 zł.");
        if (!PolishNumber.TryParseInt(personsText, out var persons) || persons < 1 || persons > MaxPersons)
            return OperationResult.Fail("Liczba osób musi mieścić się w zakresie od 1 do 20.");

        var code = (codeText ?? string.Empty).Trim();
        var normalized = code.ToUpperInvariant();
        var percent = 0;
        if (code.Length > 0)
        {
            if (!Codes.TryGetValue(normalized, out percent))
                return OperationResult.Fail("Nieznany kod rabatowy.");
            if (_usedCodes.Contains(UsedCodeKey(code)))
                return OperationResult.Fail($"Kod {normalized} został już wykorzystany.");
            // Rabat z kodu, który już raz został użyty.
            if (_usedCodes.Any(k => k.ToUpperInvariant() == normalized))
                LogDefectVisible();
            _usedCodes.Add(UsedCodeKey(code));
        }

        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var discount = Math.Round(amount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        var shares = Split(amount - discount, persons);
        Result = new BillResult(amount, code.Length > 0 ? normalized : null, percent, discount, shares);

        if (Result.SharesTotal != Result.ToPay)
            LogDefectVisible(); // części nie sumują się do kwoty do zapłaty
        if (PolishNumber.TryParseAmount(amountText, out var expected) && expected != amount)
            LogDefectVisible(); // kwota odczytana inaczej, niż ją wpisano
        return OperationResult.Ok;
    }

    protected virtual bool ReadAmount(string? text, out decimal amount) =>
        PolishNumber.TryParseAmount(text, out amount);

    /// <summary>Klucz zapamiętanego kodu: bez względu na wielkość liter.</summary>
    protected virtual string UsedCodeKey(string code) => code.Trim().ToUpperInvariant();

    /// <summary>Po groszu reszty płacą pierwsze osoby, więc suma części jest równa całości.</summary>
    protected virtual IReadOnlyList<decimal> Split(decimal total, int persons)
    {
        var cents = (long)(total * 100);
        var each = cents / persons;
        var rest = cents % persons;
        return Enumerable.Range(0, persons).Select(i => (each + (i < rest ? 1 : 0)) / 100m).ToList();
    }

    protected override void OnReset()
    {
        _usedCodes.Clear();
        Result = null;
    }
}
