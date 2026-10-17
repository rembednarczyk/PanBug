using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Loan;
using Xunit;

namespace PanBug.Domain.Tests;

public class LoanTests
{
    private sealed class CorrectLoan : LoanSimulationBase
    {
        public CorrectLoan() : base(99, PointValue.One) { }
    }

    private static LoanResult Run(LoanSimulationBase sim, string amount, string rate, string period)
    {
        sim.AmountText = amount;
        sim.RateText = rate;
        sim.PeriodText = period;
        Assert.True(sim.Calculate().Success);
        return sim.Result!;
    }

    [Fact]
    public void Task07_zero_rate_gives_payment_that_is_not_a_number()
    {
        var sim = new Task07Loan();
        var result = Run(sim, "12 000", "0", "12");
        Assert.True(double.IsNaN(result.MonthlyPayment));
        Assert.False(result.HasSchedule);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_zero_rate_divides_amount_evenly()
    {
        var sim = new CorrectLoan();
        var result = Run(sim, "12 000", "0", "12");
        Assert.Equal(1000d, result.MonthlyPayment, 6);
        Assert.Equal(0m, result.TotalInterest);
        Assert.Equal(0m, result.Schedule[^1].Remaining);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("10 000", "7,5", "12")]
    [InlineData("25 000", "8", "36")]
    public void Task08_last_installment_leaves_remainder(string amount, string rate, string months)
    {
        var sim = new Task08Loan();
        var result = Run(sim, amount, rate, months);
        Assert.NotEqual(0m, result.Schedule[^1].Remaining);
        Assert.True(sim.DefectShown);
    }

    [Theory]
    [InlineData("10 000", "7,5", "12")]
    [InlineData("25 000", "8", "36")]
    [InlineData("500 000", "20", "360")]
    [InlineData("1000", "0,1", "1")]
    public void Correct_schedule_repays_exactly_the_amount(string amount, string rate, string months)
    {
        var sim = new CorrectLoan();
        var result = Run(sim, amount, rate, months);
        Assert.Equal(0m, result.Schedule[^1].Remaining);
        Assert.Equal(result.Amount, result.TotalPrincipal);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task09_unit_change_keeps_value_and_multiplies_period()
    {
        var sim = new Task09Loan();
        sim.AmountText = "10 000";
        sim.RateText = "6";
        sim.PeriodText = "24";
        sim.SetUnit(PeriodUnit.Years);
        Assert.Equal("24", sim.PeriodText);
        Assert.True(sim.Calculate().Success);
        Assert.Equal(288, sim.Result!.Months);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Task09_switching_back_is_not_a_defect()
    {
        var sim = new Task09Loan();
        sim.AmountText = "10 000";
        sim.RateText = "6";
        sim.PeriodText = "24";
        sim.SetUnit(PeriodUnit.Years);
        sim.SetUnit(PeriodUnit.Months);
        Assert.True(sim.Calculate().Success);
        Assert.Equal(24, sim.Result!.Months);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("24", "2")]
    [InlineData("13", "2")]
    [InlineData("12", "1")]
    public void Correct_unit_change_converts_months_to_years(string months, string years)
    {
        var sim = new CorrectLoan();
        sim.PeriodText = months;
        sim.SetUnit(PeriodUnit.Years);
        Assert.Equal(years, sim.PeriodText);
        sim.AmountText = "10 000";
        sim.RateText = "6";
        Assert.True(sim.Calculate().Success);
        Assert.Equal(int.Parse(years) * 12, sim.Result!.Months);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Correct_years_entered_directly_are_not_a_defect()
    {
        var sim = new CorrectLoan();
        sim.SetUnit(PeriodUnit.Years);
        Run(sim, "10 000", "6", "3");
        Assert.Equal(36, sim.Result!.Months);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("999,99", "5", "12")]
    [InlineData("500 000,01", "5", "12")]
    [InlineData("10 000", "20,01", "12")]
    [InlineData("10 000", "5", "361")]
    [InlineData("10 000", "5", "0")]
    public void Out_of_range_input_is_rejected(string amount, string rate, string months)
    {
        var sim = new CorrectLoan { AmountText = amount, RateText = rate, PeriodText = months };
        Assert.False(sim.Calculate().Success);
        Assert.Null(sim.Result);
    }
}
