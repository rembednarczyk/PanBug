using System.Text.RegularExpressions;

namespace PanBug.Domain.Common;

/// <summary>Polski numer telefonu: 9 cyfr, opcjonalnie z kierunkowym +48 lub 0048, ze spacjami lub myślnikami.</summary>
public static class PhoneNumber
{
    public static bool TryNormalize(string? text, out string digits)
    {
        digits = string.Empty;
        var s = Regex.Replace(text ?? string.Empty, @"[\s\-]", string.Empty);
        if (s.StartsWith("+48"))
            s = s[3..];
        else if (s.StartsWith("0048"))
            s = s[4..];
        if (!Regex.IsMatch(s, @"^[0-9]{9}\z"))
            return false;
        digits = s;
        return true;
    }

    /// <summary>Zapis do wyświetlenia, np. "600 000 000".</summary>
    public static string Format(string digits) =>
        digits.Length == 9 ? $"{digits[..3]} {digits[3..6]} {digits[6..]}" : digits;
}
