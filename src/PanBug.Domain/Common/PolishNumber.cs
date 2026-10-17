using System.Globalization;
using System.Text.RegularExpressions;

namespace PanBug.Domain.Common;

/// <summary>Odczyt i zapis kwot w polskim formacie (przecinek dziesiętny, spacja lub kropka tysięcy).</summary>
public static class PolishNumber
{
    public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("pl-PL");

    private static readonly Regex ThousandsWithDots = new(@"^[0-9]{1,3}(\.[0-9]{3})+\z", RegexOptions.CultureInvariant);
    private static readonly Regex PlainNumber = new(@"^[0-9]+(\.[0-9]{1,2})?\z", RegexOptions.CultureInvariant);

    /// <summary>
    /// Kwota z najwyżej dwoma miejscami po przecinku, np. "1250", "1 250,50", "1.250,50", "12.5".
    /// Kropka bez przecinka jest separatorem tysięcy tylko w układzie grup po 3 cyfry ("1.250").
    /// </summary>
    public static bool TryParseAmount(string? text, out decimal value)
    {
        value = 0;
        var s = (text ?? string.Empty).Trim().Replace(" ", string.Empty).Replace(" ", string.Empty);
        if (s.Length == 0)
            return false;

        if (s.Contains(','))
        {
            var parts = s.Split(',');
            if (parts.Length != 2)
                return false;
            var whole = parts[0];
            if (whole.Contains('.') && !ThousandsWithDots.IsMatch(whole))
                return false;
            s = whole.Replace(".", string.Empty) + "." + parts[1];
        }
        else if (ThousandsWithDots.IsMatch(s))
        {
            s = s.Replace(".", string.Empty);
        }

        if (!PlainNumber.IsMatch(s))
            return false;
        return decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    public static bool TryParseInt(string? text, out int value)
    {
        var s = (text ?? string.Empty).Trim();
        value = 0;
        return Regex.IsMatch(s, @"^[0-9]{1,9}\z") && int.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Kwota w złotych, np. "1 250,50 zł".</summary>
    public static string Money(decimal value) => value.ToString("N2", Culture) + " zł";

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}
