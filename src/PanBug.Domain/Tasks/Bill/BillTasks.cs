using System.Globalization;
using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Bill;

/// <summary>Zadanie 19: każda osoba płaci zaokrągloną średnią, więc grosz reszty ginie albo pojawia się nadpłata.</summary>
public sealed class Task19Bill : BillSimulationBase
{
    public Task19Bill() : base(19, PointValue.One) { }

    protected override IReadOnlyList<decimal> Split(decimal total, int persons) =>
        Enumerable.Repeat(Math.Round(total / persons, 2, MidpointRounding.AwayFromZero), persons).ToList();
}

/// <summary>Zadanie 20: wykorzystany kod jest zapamiętany z wielkością liter, więc ten sam kod małymi literami działa drugi raz.</summary>
public sealed class Task20Bill : BillSimulationBase
{
    public Task20Bill() : base(20, PointValue.Two) { }

    protected override string UsedCodeKey(string code) => code.Trim();
}

/// <summary>Zadanie 21: kropka jest zawsze czytana jako separator dziesiętny, a przecinek usuwany, więc "1.000,50" to 1,0005 zł.</summary>
public sealed class Task21Bill : BillSimulationBase
{
    public Task21Bill() : base(21, PointValue.Three) { }

    protected override bool ReadAmount(string? text, out decimal amount)
    {
        var s = (text ?? string.Empty).Replace(" ", string.Empty).Replace(" ", string.Empty);
        if (s.Contains('.'))
            s = s.Replace(",", string.Empty);
        else
            s = s.Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    }
}
