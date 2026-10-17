using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Login;

/// <summary>Zadanie 13: reset hasła odpowiada inaczej dla istniejącego i nieistniejącego adresu.</summary>
public sealed class Task13Login : LoginSimulationBase
{
    public Task13Login() : base(13, PointValue.Two) { }

    protected override string ResetMessage(bool accountExists, string email) => accountExists
        ? $"Wysłaliśmy link do zmiany hasła na adres {email}."
        : "Nie znaleziono konta o podanym adresie e-mail.";
}

/// <summary>Zadanie 14: licznik prób rozróżnia wielkość liter loginu, więc "Tester" omija blokadę konta "tester".</summary>
public sealed class Task14Login : LoginSimulationBase
{
    public Task14Login() : base(14, PointValue.Three) { }

    protected override string CounterKey(string login) => login.Trim();
}

/// <summary>Zadanie 15: po wylogowaniu hasło zostaje w formularzu logowania.</summary>
public sealed class Task15Login : LoginSimulationBase
{
    public Task15Login() : base(15, PointValue.Two) { }

    protected override void ClearPasswordOnLogout() { }
}
