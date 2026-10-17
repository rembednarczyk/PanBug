using System.Globalization;
using System.Text.RegularExpressions;
using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Parcel;

/// <summary>Zadanie 4: paczka o wysokości równej limitowi jest odrzucana.</summary>
public sealed class Task04Parcel : ParcelSimulationBase
{
    public Task04Parcel() : base(4, PointValue.One) { }

    protected override bool HeightAllowed(int height) => height < MaxHeight;
}

/// <summary>Zadanie 5: wpisany kod jest porównywany jako liczba, więc kod z zerem na początku nie pasuje.</summary>
public sealed class Task05Parcel : ParcelSimulationBase
{
    public Task05Parcel() : base(5, PointValue.Two) { }

    protected override bool CodesMatch(string stored, string entered) =>
        int.Parse(entered, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture) == stored;
}

/// <summary>Zadanie 6: telefon jest sprawdzany jako 9 cyfr bez spacji i kierunkowego, choć podpowiedź pokazuje +48 i spacje.</summary>
public sealed class Task06Parcel : ParcelSimulationBase
{
    public Task06Parcel() : base(6, PointValue.One) { }

    protected override bool TryReadPhone(string? text, out string digits)
    {
        digits = (text ?? string.Empty).Trim();
        return Regex.IsMatch(digits, "^[0-9]{9}\\z");
    }
}
