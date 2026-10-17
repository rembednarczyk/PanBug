using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Login;
using Xunit;

namespace PanBug.Domain.Tests;

public class LoginTests
{
    private sealed class CorrectLogin : LoginSimulationBase
    {
        public CorrectLogin() : base(99, PointValue.One) { }
    }

    private static bool Attempt(LoginSimulationBase sim, string login, string password)
    {
        sim.LoginText = login;
        sim.PasswordText = password;
        return sim.Login().Success;
    }

    private static void LockAndRetryWithCapital(LoginSimulationBase sim)
    {
        for (int i = 0; i < LoginSimulationBase.MaxAttempts; i++)
            Assert.False(Attempt(sim, "tester", "zle-haslo"));
        Attempt(sim, "Tester", LoginSimulationBase.AccountPassword);
    }

    [Fact]
    public void Task13_reset_message_reveals_whether_account_exists()
    {
        var sim = new Task13Login();
        var known = sim.RequestReset("tester@example.pl");
        var unknown = new Task13Login().RequestReset("nikt@example.pl");
        Assert.NotEqual(known.Message, unknown.Message);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_reset_message_is_identical()
    {
        var sim = new CorrectLogin();
        Assert.Equal(sim.RequestReset("tester@example.pl").Message, sim.RequestReset("nikt@example.pl").Message);
        Assert.False(sim.RequestReset("bez-malpy").Success);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task14_capitalised_login_bypasses_lock()
    {
        var sim = new Task14Login();
        LockAndRetryWithCapital(sim);
        Assert.Equal(LoginScreen.LoggedIn, sim.Screen);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_lock_applies_to_every_letter_case()
    {
        var sim = new CorrectLogin();
        LockAndRetryWithCapital(sim);
        Assert.Equal(LoginScreen.Login, sim.Screen);
        Assert.False(Attempt(sim, "TESTER", LoginSimulationBase.AccountPassword));
        sim.Unlock();
        Assert.True(Attempt(sim, "TESTER", LoginSimulationBase.AccountPassword));
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task15_password_stays_in_form_after_logout()
    {
        var sim = new Task15Login();
        Assert.True(Attempt(sim, "tester", LoginSimulationBase.AccountPassword));
        sim.Logout();
        Assert.Equal(LoginSimulationBase.AccountPassword, sim.PasswordText);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_logout_clears_form()
    {
        var sim = new CorrectLogin();
        Assert.True(Attempt(sim, "tester", LoginSimulationBase.AccountPassword));
        sim.Logout();
        Assert.Equal(string.Empty, sim.LoginText);
        Assert.Equal(string.Empty, sim.PasswordText);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Remaining_attempts_are_reported()
    {
        var sim = new CorrectLogin();
        sim.LoginText = "tester";
        sim.PasswordText = "x";
        Assert.Contains("Pozostałe próby: 2", sim.Login().Message);
    }
}
