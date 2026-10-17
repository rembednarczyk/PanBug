namespace PanBug.Domain.Common;

public static class Utils
{
    /// <summary>
    /// Polska odmiana liczebników: 1 -> text1, 2-4 (poza 12-14) -> text2, pozostałe -> text3.
    /// </summary>
    public static string PluralPl(int number, string text1, string text2, string text3)
    {
        long n = Math.Abs((long)number); // Math.Abs(int.MinValue) przepełnia int
        int form = n != 1
            ? (n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20) ? 1 : 2)
            : 0;
        return form switch
        {
            2 => text3,
            1 => text2,
            _ => text1,
        };
    }

    /// <summary>
    /// Czas gry jako gg:mm:ss (opcjonalnie z milisekundami). Godziny liczone łącznie,
    /// więc czas powyżej doby nie zawija się do zera jak w formacie "hh".
    /// </summary>
    public static string FormatDuration(TimeSpan duration, bool milliseconds = false)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;
        var text = $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        return milliseconds ? $"{text}.{duration.Milliseconds:000}" : text;
    }
}
