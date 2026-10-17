using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Bill;
using Xunit;

namespace PanBug.Domain.Tests;

public class BillTests
{
    private sealed class CorrectBill : BillSimulationBase
    {
        public CorrectBill() : base(99, PointValue.One) { }
    }

    [Fact]
    public void Task04_shares_do_not_add_up_to_total()
    {
        var sim = new Task04Bill();
        Assert.True(sim.Calculate("100", "3", "").Success);
        Assert.Equal(99.99m, sim.Result!.SharesTotal);
        Assert.True(sim.DefectShown);
    }

    [Theory]
    [InlineData("100", "3")]
    [InlineData("0,01", "20")]
    [InlineData("1 250,50", "7")]
    [InlineData("10 000", "20")]
    public void Correct_shares_add_up_and_remainder_goes_first(string amount, string persons)
    {
        var sim = new CorrectBill();
        Assert.True(sim.Calculate(amount, persons, "").Success);
        var result = sim.Result!;
        Assert.Equal(result.ToPay, result.SharesTotal);
        Assert.True(result.Shares.Zip(result.Shares.Skip(1)).All(p => p.First >= p.Second));
        Assert.True(result.Shares[0] - result.Shares[^1] <= 0.01m);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task11_used_code_works_again_in_lower_case()
    {
        var sim = new Task11Bill();
        Assert.True(sim.Calculate("200", "2", "TESTER10").Success);
        Assert.True(sim.Calculate("200", "2", "tester10").Success);
        Assert.Equal(20m, sim.Result!.Discount);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_code_can_be_used_once_in_any_case()
    {
        var sim = new CorrectBill();
        Assert.True(sim.Calculate("200", "2", "tester10").Success);
        Assert.False(sim.Calculate("200", "2", "TESTER10").Success);
        Assert.False(sim.Calculate("200", "2", "Tester10").Success);
        Assert.False(sim.Calculate("200", "2", "XYZ").Success);
        Assert.True(sim.Calculate("200", "2", "OBIAD15").Success);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("1.000,50")]
    [InlineData("1.250,00")]
    public void Task21_dot_thousands_read_as_decimal(string amount)
    {
        var sim = new Task21Bill();
        Assert.True(sim.Calculate(amount, "1", "").Success);
        Assert.True(sim.Result!.Amount < 2m);
        Assert.True(sim.DefectShown);
    }

    [Theory]
    [InlineData("1 000,50", 1000.50)]
    [InlineData("12,5", 12.5)]
    public void Task21_other_formats_are_read_correctly(string amount, double expected)
    {
        var sim = new Task21Bill();
        Assert.True(sim.Calculate(amount, "1", "").Success);
        Assert.Equal((decimal)expected, sim.Result!.Amount);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("1.000,50", 1000.50)]
    [InlineData("1 000,50", 1000.50)]
    [InlineData("1000,5", 1000.5)]
    [InlineData("1.250", 1250)]
    [InlineData("12.5", 12.5)]
    public void Correct_reads_polish_amount_formats(string amount, double expected)
    {
        var sim = new CorrectBill();
        Assert.True(sim.Calculate(amount, "1", "").Success);
        Assert.Equal((decimal)expected, sim.Result!.Amount);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("0", "2")]
    [InlineData("10 000,01", "2")]
    [InlineData("1,234", "2")]
    [InlineData("100", "0")]
    [InlineData("100", "21")]
    public void Out_of_range_input_is_rejected(string amount, string persons)
    {
        var sim = new CorrectBill();
        Assert.False(sim.Calculate(amount, persons, "").Success);
    }
}
