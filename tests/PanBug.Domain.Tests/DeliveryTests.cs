using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Delivery;
using Xunit;

namespace PanBug.Domain.Tests;

public class DeliveryTests
{
    private sealed class CorrectDelivery : DeliverySimulationBase
    {
        public CorrectDelivery() : base(99, PointValue.One) { }
    }

    private static void Fill(DeliverySimulationBase sim, string notes = "")
    {
        sim.FirstName = "Anna";
        sim.LastName = "Nowak";
        sim.Street = "Testowa 1";
        sim.PostalCode = "00-950";
        sim.City = "Warszawa";
        sim.Phone = "+48 600 000 000";
        sim.Notes = notes;
    }

    [Fact]
    public void Task22_errors_without_text_description()
    {
        var sim = new Task22Delivery();
        Assert.False(sim.Submit().Success);
        Assert.False(sim.DescribesErrorsWithText);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_errors_are_described_per_field()
    {
        var sim = new CorrectDelivery();
        sim.PostalCode = "00950";
        Assert.False(sim.Submit().Success);
        Assert.Equal(6, sim.Errors.Count);
        Assert.Equal("Kod pocztowy ma format 00-000.", sim.Errors[DeliveryField.PostalCode]);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task23_last_name_label_points_to_first_name()
    {
        var sim = new Task23Delivery();
        Assert.Equal(DeliverySimulationBase.FieldId(DeliveryField.FirstName), sim.LabelTarget(DeliveryField.LastName));
        sim.LabelClicked(DeliveryField.FirstName);
        Assert.False(sim.DefectShown);
        sim.LabelClicked(DeliveryField.LastName);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Correct_every_label_points_to_its_field()
    {
        var sim = new CorrectDelivery();
        foreach (var field in Enum.GetValues<DeliveryField>())
        {
            Assert.Equal(DeliverySimulationBase.FieldId(field), sim.LabelTarget(field));
            sim.LabelClicked(field);
        }
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Task24_long_notes_are_cut_on_save()
    {
        var sim = new Task24Delivery();
        Fill(sim, new string('a', 300));
        Assert.True(sim.Submit().Success);
        Assert.Equal(Task24Delivery.StoredNotesLength, sim.Order!.Notes.Length);
        Assert.True(sim.DefectShown);
    }

    [Fact]
    public void Task24_short_notes_are_saved_whole()
    {
        var sim = new Task24Delivery();
        Fill(sim, new string('a', 255));
        Assert.True(sim.Submit().Success);
        Assert.False(sim.DefectShown);
    }

    [Fact]
    public void Correct_saves_notes_up_to_limit()
    {
        var sim = new CorrectDelivery();
        Fill(sim, new string('a', DeliverySimulationBase.NotesMaxLength));
        Assert.True(sim.Submit().Success);
        Assert.Equal(DeliverySimulationBase.NotesMaxLength, sim.Order!.Notes.Length);
        Assert.Equal("600 000 000", sim.Order.Phone);
        Assert.False(sim.DefectShown);
        sim.NewOrder();
        Assert.Null(sim.Order);
        Assert.Equal(string.Empty, sim.FirstName);
    }
}
