using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Parcel;
using Xunit;

namespace PanBug.Domain.Tests;

public class ParcelTests
{
    private sealed class CorrectParcel : ParcelSimulationBase
    {
        public CorrectParcel() : base(99, PointValue.One) { }
    }

    private static ParcelInput Valid(string height = "30", string phone = "600000000") =>
        new(height, "30", "30", "2,5", phone);

    [Fact]
    public void Task04_height_equal_to_limit_is_rejected()
    {
        var sim = new Task04Parcel();
        Assert.False(sim.Send(Valid(height: "64")).Success);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_accepts_every_limit_and_rejects_just_above()
    {
        var sim = new CorrectParcel();
        Assert.True(sim.Send(new ParcelInput("64", "38", "41", "25", "600000000")).Success);
        Assert.True(sim.Send(new ParcelInput("1", "1", "1", "0,1", "600000000")).Success);
        Assert.False(sim.Send(Valid(height: "65")).Success);
        Assert.False(sim.Send(new ParcelInput("30", "39", "30", "1", "600000000")).Success);
        Assert.False(sim.Send(new ParcelInput("30", "30", "42", "1", "600000000")).Success);
        Assert.False(sim.Send(new ParcelInput("30", "30", "30", "25,01", "600000000")).Success);
        Assert.False(sim.Send(new ParcelInput("30", "30", "30", "0,09", "600000000")).Success);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task05_code_with_leading_zero_does_not_open_locker()
    {
        var sim = new Task05Parcel();
        Assert.True(sim.Send(Valid()).Success);
        Assert.Equal("048213", sim.Parcels[0].Code);
        Assert.False(sim.Collect("600 000 000", "048213").Success);
        Assert.True(sim.DefectShown);
        Assert.False(sim.Parcels[0].Collected);
    }

    [Fact]
    public void Task05_code_without_leading_zero_still_works()
    {
        var sim = new Task05Parcel();
        sim.Send(Valid());
        sim.Send(Valid(phone: "600000001"));
        Assert.True(sim.Collect("600000001", "731905").Success);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Correct_opens_locker_for_code_with_leading_zero()
    {
        var sim = new CorrectParcel();
        sim.Send(Valid());
        Assert.True(sim.Collect("+48 600 000 000", "048213").Success);
        Assert.True(sim.Parcels[0].Collected);
        Assert.False(sim.Collect("600000000", "048213").Success); // już odebrana
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("+48 600 000 000")]
    [InlineData("600 000 000")]
    [InlineData("+48600000000")]
    public void Task06_phone_in_hint_format_is_rejected(string phone)
    {
        var sim = new Task06Parcel();
        Assert.False(sim.Send(Valid(phone: phone)).Success);
        Assert.True(sim.DefectShown);
    }

    [Theory]
    [InlineData("+48 600 000 000")]
    [InlineData("600 000 000")]
    [InlineData("0048600000000")]
    [InlineData("600-000-000")]
    public void Correct_accepts_phone_formats(string phone)
    {
        var sim = new CorrectParcel();
        Assert.True(sim.Send(Valid(phone: phone)).Success);
        Assert.Equal("600000000", sim.Parcels[0].Phone);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [InlineData("60000000")]
    [InlineData("+49 600 000 000")]
    [InlineData("abc")]
    public void Invalid_phone_rejected_without_marker(string phone)
    {
        var sim = new Task06Parcel();
        Assert.False(sim.Send(Valid(phone: phone)).Success);
        Assert.False(sim.DefectShown);
    }
}
