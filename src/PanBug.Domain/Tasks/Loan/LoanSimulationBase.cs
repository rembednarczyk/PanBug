using PanBug.Domain.Common;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Loan;

public enum PeriodUnit { Months, Years }

public sealed record Installment(int Number, decimal Payment, decimal Principal, decimal Interest, decimal Remaining);

/// <summary>Wynik obliczenia. Rata jest liczbą zmiennoprzecinkową, bo tak liczy ją wzór na ratę równą.</summary>
public sealed record LoanResult(decimal Amount, int Months, double MonthlyPayment, IReadOnlyList<Installment> Schedule)
{
    public bool HasSchedule => Schedule.Count > 0;
    public decimal TotalPaid => Schedule.Sum(i => i.Payment);
    public decimal TotalInterest => Schedule.Sum(i => i.Interest);
    public decimal TotalPrincipal => Schedule.Sum(i => i.Principal);
}

/// <summary>Kalkulator raty kredytu w ratach równych z harmonogramem spłaty.</summary>
public abstract class LoanSimulationBase : TaskBase
{
    public const decimal MinAmount = 1_000m;
    public const decimal MaxAmount = 500_000m;
    public const decimal MaxRate = 20m;
    public const int MaxMonths = 360;

    public const string Spec =
        "Kalkulator raty kredytu (raty równe).\n\n" +
        "Wymagania:\n" +
        "• Kwota kredytu od 1 000 do 500 000 zł, oprocentowanie roczne od 0 do 20%. Oprocentowanie 0% jest dozwolone (np. promocja bez odsetek).\n" +
        "• Okres podaje się w miesiącach (1–360) albo w latach (1–30). Zmiana jednostki przelicza wpisaną wartość, np. 24 miesiące → 2 lata. Niepełny rok zaokrągla się w górę.\n" +
        "• Harmonogram pokazuje wszystkie raty. Suma części kapitałowych jest równa kwocie kredytu, a po ostatniej racie do spłaty zostaje 0,00 zł. Ostatnia rata może różnić się od pozostałych o kilka groszy.";

    private string _periodText = string.Empty;
    // Jednostka, w której wpisano wartość okresu. Inna niż bieżąca = wartość nieprzeliczona.
    private PeriodUnit _enteredUnit = PeriodUnit.Months;

    protected LoanSimulationBase(uint number, PointValue points) : base(number, points, Spec) { }

    public string AmountText { get; set; } = string.Empty;
    public string RateText { get; set; } = string.Empty;

    public string PeriodText
    {
        get => _periodText;
        set
        {
            _periodText = value ?? string.Empty;
            _enteredUnit = Unit;
        }
    }

    public PeriodUnit Unit { get; private set; } = PeriodUnit.Months;
    public LoanResult? Result { get; private set; }

    public void SetUnit(PeriodUnit unit)
    {
        if (unit == Unit)
            return;
        Unit = unit;
        if (!ConvertsPeriodOnUnitChange)
            return; // etykieta jednostki się zmienia, wartość w polu nie
        if (PolishNumber.TryParseInt(_periodText, out var value) && value > 0)
            _periodText = (unit == PeriodUnit.Years ? (int)Math.Ceiling(value / 12.0) : value * 12).ToString();
        _enteredUnit = unit;
    }

    public OperationResult Calculate()
    {
        Result = null;
        if (!PolishNumber.TryParseAmount(AmountText, out var amount) || amount < MinAmount || amount > MaxAmount)
            return OperationResult.Fail("Kwota kredytu musi mieścić się w zakresie od 1 000 do 500 000 zł.");
        if (!PolishNumber.TryParseAmount(RateText, out var rate) || rate > MaxRate)
            return OperationResult.Fail("Oprocentowanie musi mieścić się w zakresie od 0 do 20%.");
        if (!PolishNumber.TryParseInt(PeriodText, out var period) || period < 1)
            return OperationResult.Fail("Okres kredytu musi mieścić się w zakresie od 1 do 360 miesięcy (od 1 do 30 lat).");
        var months = Unit == PeriodUnit.Years ? period * 12L : period;
        if (months > MaxMonths)
            return OperationResult.Fail("Okres kredytu musi mieścić się w zakresie od 1 do 360 miesięcy (od 1 do 30 lat).");

        if (Unit != _enteredUnit)
            LogDefectVisible(); // harmonogram dla wartości wpisanej w innej jednostce

        var payment = ComputePayment(amount, rate, (int)months);
        if (!double.IsFinite(payment))
        {
            LogDefectVisible(); // rata, która nie jest liczbą
            Result = new LoanResult(amount, (int)months, payment, Array.Empty<Installment>());
            return OperationResult.Ok;
        }

        var schedule = BuildSchedule(amount, rate, (int)months, Math.Round((decimal)payment, 2, MidpointRounding.AwayFromZero));
        Result = new LoanResult(amount, (int)months, payment, schedule);
        if (schedule[^1].Remaining != 0)
            LogDefectVisible(); // po ostatniej racie zostaje dług albo nadpłata
        return OperationResult.Ok;
    }

    /// <summary>Rata równa: P·r / (1 − (1 + r)^−n), a dla 0% po prostu P / n.</summary>
    protected virtual double ComputePayment(decimal amount, decimal annualRatePercent, int months)
    {
        if (annualRatePercent == 0)
            return (double)amount / months;
        return Annuity(amount, annualRatePercent, months);
    }

    protected static double Annuity(decimal amount, decimal annualRatePercent, int months)
    {
        var r = (double)annualRatePercent / 100 / 12;
        return (double)amount * r / (1 - Math.Pow(1 + r, -months));
    }

    /// <summary>Czy ostatnia rata domyka kapitał (wyrównanie groszy z zaokrągleń).</summary>
    protected virtual bool AdjustsLastInstallment => true;

    protected virtual bool ConvertsPeriodOnUnitChange => true;

    private List<Installment> BuildSchedule(decimal amount, decimal annualRatePercent, int months, decimal payment)
    {
        var monthlyRate = annualRatePercent / 100m / 12m;
        var remaining = amount;
        var list = new List<Installment>(months);
        for (int i = 1; i <= months; i++)
        {
            var interest = Math.Round(remaining * monthlyRate, 2, MidpointRounding.AwayFromZero);
            var principal = payment - interest;
            if (i == months && AdjustsLastInstallment)
                principal = remaining;
            remaining -= principal;
            list.Add(new Installment(i, principal + interest, principal, interest, remaining));
        }
        return list;
    }

    protected override void OnReset()
    {
        AmountText = string.Empty;
        RateText = string.Empty;
        Unit = PeriodUnit.Months;
        PeriodText = string.Empty;
        Result = null;
    }
}
