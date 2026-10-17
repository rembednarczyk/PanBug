using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Delivery;

/// <summary>Zadanie 8: błędne pole ma tylko czerwoną ramkę, bez tekstu błędu i bez informacji dla czytnika ekranu.</summary>
public sealed class Task08Delivery : DeliverySimulationBase
{
    public Task08Delivery() : base(8, PointValue.Two) { }

    public override bool DescribesErrorsWithText => false;
}

/// <summary>Zadanie 17: etykieta „Nazwisko” wskazuje pole „Imię”.</summary>
public sealed class Task17Delivery : DeliverySimulationBase
{
    public Task17Delivery() : base(17, PointValue.Two) { }

    public override string LabelTarget(DeliveryField field) =>
        FieldId(field == DeliveryField.LastName ? DeliveryField.FirstName : field);
}

/// <summary>Zadanie 24: licznik dopuszcza 500 znaków, a zapis obcina uwagi do 255.</summary>
public sealed class Task24Delivery : DeliverySimulationBase
{
    public const int StoredNotesLength = 255;

    public Task24Delivery() : base(24, PointValue.Three) { }

    protected override string StoreNotes(string notes) =>
        notes.Length > StoredNotesLength ? notes[..StoredNotesLength] : notes;
}
