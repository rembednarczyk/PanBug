using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Loan;

/// <summary>Zadanie 7: dla oprocentowania 0% wzór dzieli zero przez zero i rata nie jest liczbą.</summary>
public sealed class Task07Loan : LoanSimulationBase
{
    public Task07Loan() : base(7, PointValue.Two) { }

    protected override double ComputePayment(decimal amount, decimal annualRatePercent, int months) =>
        Annuity(amount, annualRatePercent, months);
}

/// <summary>Zadanie 14: ostatnia rata nie wyrównuje groszy z zaokrągleń, więc kapitał nie domyka się do zera.</summary>
public sealed class Task14Loan : LoanSimulationBase
{
    public Task14Loan() : base(14, PointValue.Three) { }

    protected override bool AdjustsLastInstallment => false;
}

/// <summary>Zadanie 20: zmiana jednostki okresu zmienia tylko etykietę, wartość w polu zostaje bez przeliczenia.</summary>
public sealed class Task20Loan : LoanSimulationBase
{
    public Task20Loan() : base(20, PointValue.Two) { }

    protected override bool ConvertsPeriodOnUnitChange => false;
}
