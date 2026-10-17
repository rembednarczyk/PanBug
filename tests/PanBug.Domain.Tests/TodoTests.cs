using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Todo;
using Xunit;

namespace PanBug.Domain.Tests;

public class TodoTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);

    private sealed class CorrectTodo : TodoSimulationBase
    {
        public CorrectTodo(DateOnly today) : base(99, PointValue.One, today) { }
    }

    /// <summary>Każdy dzień roku przestępnego, żeby defekty nie zależały od daty gry.</summary>
    public static IEnumerable<object[]> EveryDay() =>
        Enumerable.Range(0, 366).Select(i => new object[] { new DateOnly(2028, 1, 1).AddDays(i) });

    [Theory]
    [MemberData(nameof(EveryDay))]
    public void Task16_task_due_today_is_listed_as_overdue(DateOnly today)
    {
        var sim = new Task16Todo(today);
        sim.SetFilter(TodoFilter.Overdue);
        Assert.Contains(sim.Visible, i => i.Due == today);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_overdue_filter_shows_only_past_open_tasks()
    {
        var sim = new CorrectTodo(Today);
        sim.SetFilter(TodoFilter.Overdue);
        var item = Assert.Single(sim.Visible);
        Assert.Equal(Today.AddDays(-2), item.Due);
        sim.ToggleDone(item);
        Assert.Empty(sim.Visible);
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [MemberData(nameof(EveryDay))]
    public void Task17_sort_orders_by_day_of_month(DateOnly today)
    {
        var sim = new Task17Todo(today);
        sim.SortByDue();
        Assert.True(sim.DefectShown);
    }

    [Theory]
    [MemberData(nameof(EveryDay))]
    public void Correct_sort_is_chronological(DateOnly today)
    {
        var sim = new CorrectTodo(today);
        sim.SortByDue();
        Assert.Equal(sim.Items.OrderBy(i => i.Due).Select(i => i.Due), sim.Items.Select(i => i.Due));
        Assert.False(sim.DefectShown);
    }

    [Theory]
    [MemberData(nameof(EveryDay))]
    public void Task18_delete_in_filtered_view_removes_other_task(DateOnly today)
    {
        var sim = new Task18Todo(today);
        sim.SetFilter(TodoFilter.Overdue);
        var clicked = sim.Visible[0];
        sim.DeleteVisibleAt(0);
        Assert.Contains(clicked, sim.Items);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_delete_in_filtered_view_removes_clicked_task()
    {
        var sim = new CorrectTodo(Today);
        sim.SetFilter(TodoFilter.Done);
        var clicked = sim.Visible[0];
        sim.DeleteVisibleAt(0);
        Assert.DoesNotContain(clicked, sim.Items);
        Assert.Equal(5, sim.Items.Count);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Adding_task_validates_input()
    {
        var sim = new CorrectTodo(Today);
        Assert.False(sim.Add("", "2026-10-20").Success);
        Assert.False(sim.Add("Zadanie", "").Success);
        Assert.False(sim.Add(new string('x', 101), "2026-10-20").Success);
        Assert.True(sim.Add("Zadanie", "2026-10-20").Success);
        Assert.Equal(7, sim.Items.Count);
    }
}
