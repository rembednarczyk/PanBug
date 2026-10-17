using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Login;

/// <summary>Zadanie 5: reset hasła odpowiada inaczej dla istniejącego i nieistniejącego adresu.</summary>
public sealed class Task05Login : LoginSimulationBase
{
    public Task05Login() : base(5, PointValue.Two) { }

    protected override string ResetMessage(bool accountExists, string email) => accountExists
        ? $"Wysłaliśmy link do zmiany hasła na adres {email}."
        : "Nie znaleziono konta o podanym adresie e-mail.";
}

/// <summary>Zadanie 12: licznik prób rozróżnia wielkość liter loginu, więc "Tester" omija blokadę konta "tester".</summary>
public sealed class Task12Login : LoginSimulationBase
{
    public Task12Login() : base(12, PointValue.Three) { }

    protected override string CounterKey(string login) => login.Trim();
}

/// <summary>Zadanie 19: po wylogowaniu hasło zostaje w formularzu logowania.</summary>
public sealed class Task19Login : LoginSimulationBase
{
    public Task19Login() : base(19, PointValue.Two) { }

    protected override void ClearPasswordOnLogout() { }
}
