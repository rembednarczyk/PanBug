using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Leave;
using Xunit;

namespace PanBug.Domain.Tests;

public class LeaveTests
{
    internal static readonly DateOnly Today = new(2026, 10, 14);

    private sealed class CorrectLeave : LeaveSimulationBase
    {
        public CorrectLeave() : base(99, PointValue.One, LeaveTests.Today) { }
    }

    [Fact]
    public void Task10_weekend_days_are_counted()
    {
        var sim = new Task10Leave(Today);
        Assert.True(sim.Submit("2026-11-02", "2026-11-08").Success); // pon.-niedz.
        Assert.Equal(7, sim.Used(2026));
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_counts_only_working_days()
    {
        var sim = new CorrectLeave();
        Assert.True(sim.Submit("02.11.2026", "08.11.2026").Success);
        Assert.Equal(5, sim.Used(2026));
        Assert.False(sim.Submit("2026-11-14", "2026-11-15").Success); // sam weekend
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task11_year_end_request_takes_all_days_from_first_year()
    {
        var sim = new Task11Leave(Today);
        Assert.True(sim.Submit("2026-12-28", "2027-01-08").Success);
        Assert.Equal(10, sim.Used(2026));
        Assert.Equal(0, sim.Used(2027));
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_year_end_request_is_split_between_years()
    {
        var sim = new CorrectLeave();
        Assert.True(sim.Submit("2026-12-28", "2027-01-08").Success);
        Assert.Equal(4, sim.Used(2026));
        Assert.Equal(6, sim.Used(2027));
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task12_cancelling_approved_request_keeps_days_reserved()
    {
        var sim = new Task12Leave(Today);
        sim.Submit("2026-11-02", "2026-11-06");
        var request = sim.Requests[0];
        sim.Approve(request);
        sim.Cancel(request);
        Assert.Equal(LeaveStatus.Cancelled, request.Status);
        Assert.Equal(5, sim.Used(2026));
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Task12_cancelling_pending_request_returns_days()
    {
        var sim = new Task12Leave(Today);
        sim.Submit("2026-11-02", "2026-11-06");
        sim.Cancel(sim.Requests[0]);
        Assert.Equal(0, sim.Used(2026));
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Correct_cancelling_approved_request_returns_days()
    {
        var sim = new CorrectLeave();
        sim.Submit("2026-11-02", "2026-11-06");
        sim.Approve(sim.Requests[0]);
        sim.Cancel(sim.Requests[0]);
        sim.Cancel(sim.Requests[0]); // drugie anulowanie nic nie zmienia
        Assert.Equal(0, sim.Used(2026));
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Pool_limit_and_date_rules_are_enforced()
    {
        var sim = new CorrectLeave();
        Assert.False(sim.Submit("2026-11-10", "2026-11-02").Success); // odwrócone daty
        Assert.False(sim.Submit("2025-12-29", "2025-12-30").Success); // poprzedni rok
        Assert.False(sim.Submit("2028-01-03", "2028-01-04").Success); // za dwa lata
        Assert.False(sim.Submit("", "2026-11-04").Success);
        Assert.True(sim.Submit("2027-01-04", "2027-02-08").Success); // 26 dni roboczych
        Assert.Equal(0, sim.Remaining(2027));
        Assert.False(sim.Submit("2027-03-01", "2027-03-01").Success);
        Assert.False(sim.DefectShown);
    }
}
