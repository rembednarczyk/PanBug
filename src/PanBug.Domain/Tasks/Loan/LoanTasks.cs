using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Loan;

/// <summary>Zadanie 7: dla oprocentowania 0% wzór dzieli zero przez zero i rata nie jest liczbą.</summary>
public sealed class Task07Loan : LoanSimulationBase
{
    public Task07Loan() : base(7, PointValue.Two) { }

    protected override double ComputePayment(decimal amount, decimal annualRatePercent, int months) =>
        Annuity(amount, annualRatePercent, months);
}

/// <summary>Zadanie 8: ostatnia rata nie wyrównuje groszy z zaokrągleń, więc kapitał nie domyka się do zera.</summary>
public sealed class Task08Loan : LoanSimulationBase
{
    public Task08Loan() : base(8, PointValue.Three) { }

    protected override bool AdjustsLastInstallment => false;
}

/// <summary>Zadanie 9: zmiana jednostki okresu zmienia tylko etykietę, wartość w polu zostaje bez przeliczenia.</summary>
public sealed class Task09Loan : LoanSimulationBase
{
    public Task09Loan() : base(9, PointValue.Two) { }

    protected override bool ConvertsPeriodOnUnitChange => false;
}
