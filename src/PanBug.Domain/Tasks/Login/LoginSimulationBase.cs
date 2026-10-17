using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Login;

public enum LoginScreen { Login, LoggedIn, Reset }

/// <summary>Logowanie z blokadą konta po nieudanych próbach i resetem hasła.</summary>
public abstract class LoginSimulationBase : TaskBase
{
    public const string AccountLogin = "tester";
    public const string AccountPassword = "Pr0jekt#2026";
    public const string AccountEmail = "tester@example.pl";
    public const int MaxAttempts = 3;

    public const string Spec =
        "Logowanie do serwisu z resetem hasła.\n\n" +
        "Dane testowe: login tester, hasło Pr0jekt#2026, e-mail tester@example.pl.\n\n" +
        "Wymagania:\n" +
        "• Login nie rozróżnia wielkości liter (tester, Tester i TESTER to to samo konto).\n" +
        "• Trzy nieudane próby logowania blokują konto. Zablokowanego konta nie da się użyć, dopóki administrator go nie odblokuje (przycisk „Odblokuj konto” symuluje administratora).\n" +
        "• Komunikat po prośbie o reset hasła nie może zdradzać, czy konto o podanym adresie istnieje.\n" +
        "• Po wylogowaniu formularz logowania jest pusty.";

    // Liczniki nieudanych prób według klucza konta.
    private readonly Dictionary<string, int> _failures = new();

    protected LoginSimulationBase(uint number, PointValue points) : base(number, points, Spec) { }

    public string LoginText { get; set; } = string.Empty;
    public string PasswordText { get; set; } = string.Empty;
    public LoginScreen Screen { get; private set; }
    public string? LoggedInAs { get; private set; }

    public void ShowReset() => Screen = LoginScreen.Reset;
    public void ShowLogin() => Screen = LoginScreen.Login;

    public OperationResult Login()
    {
        var login = LoginText.Trim();
        if (login.Length == 0 || PasswordText.Length == 0)
            return OperationResult.Fail("Podaj login i hasło.");

        var isAccount = Normalize(login) == AccountLogin;
        var key = CounterKey(login);
        var failures = _failures.GetValueOrDefault(key);
        if (failures >= MaxAttempts)
            return OperationResult.Fail("Konto jest zablokowane po 3 nieudanych próbach logowania. Skontaktuj się z administratorem.");

        // Próba na koncie, które powinno być zablokowane.
        if (isAccount && AccountFailures() >= MaxAttempts)
            LogDefectVisible();

        if (!isAccount || PasswordText != AccountPassword)
        {
            _failures[key] = ++failures;
            var left = MaxAttempts - failures;
            return OperationResult.Fail(left > 0
                ? $"Nieprawidłowy login lub hasło. Pozostałe próby: {left}."
                : "Nieprawidłowy login lub hasło. Konto zostało zablokowane.");
        }

        _failures[key] = 0;
        LoggedInAs = AccountLogin;
        Screen = LoginScreen.LoggedIn;
        return OperationResult.Ok;
    }

    public void Logout()
    {
        LoggedInAs = null;
        Screen = LoginScreen.Login;
        LoginText = string.Empty;
        ClearPasswordOnLogout();
        if (PasswordText.Length > 0)
            LogDefectVisible(); // hasło poprzedniej sesji zostało w formularzu
    }

    public void Unlock() => _failures.Clear();

    public OperationResult RequestReset(string? email)
    {
        var address = (email ?? string.Empty).Trim();
        var at = address.IndexOf('@');
        if (at < 1 || address.IndexOf('.', at) < at + 2 || address.EndsWith('.'))
            return OperationResult.Fail("Podaj poprawny adres e-mail.");

        var exists = string.Equals(address, AccountEmail, StringComparison.OrdinalIgnoreCase);
        // Różne komunikaty dla istniejącego i nieistniejącego konta zdradzają, kto ma konto.
        if (ResetMessage(true, address) != ResetMessage(false, address))
            LogDefectVisible();
        return new OperationResult(true, ResetMessage(exists, address));
    }

    protected static string Normalize(string login) => login.Trim().ToLowerInvariant();

    /// <summary>Klucz licznika prób: to samo konto bez względu na wielkość liter.</summary>
    protected virtual string CounterKey(string login) => Normalize(login);

    protected virtual void ClearPasswordOnLogout() => PasswordText = string.Empty;

    protected virtual string ResetMessage(bool accountExists, string email) =>
        "Jeśli konto z tym adresem istnieje, wysłaliśmy na nie link do zmiany hasła.";

    private int AccountFailures() =>
        _failures.Where(f => Normalize(f.Key) == AccountLogin).Sum(f => f.Value);

    protected override void OnReset()
    {
        _failures.Clear();
        LoginText = string.Empty;
        PasswordText = string.Empty;
        LoggedInAs = null;
        Screen = LoginScreen.Login;
    }
}
