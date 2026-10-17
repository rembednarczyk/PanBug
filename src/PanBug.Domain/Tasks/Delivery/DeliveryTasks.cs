using PanBug.Domain.Model;

namespace PanBug.Domain.Tasks.Delivery;

/// <summary>Zadanie 22: błędne pole ma tylko czerwoną ramkę, bez tekstu błędu i bez informacji dla czytnika ekranu.</summary>
public sealed class Task22Delivery : DeliverySimulationBase
{
    public Task22Delivery() : base(22, PointValue.Two) { }

    public override bool DescribesErrorsWithText => false;
}

/// <summary>Zadanie 23: etykieta „Nazwisko” wskazuje pole „Imię”.</summary>
public sealed class Task23Delivery : DeliverySimulationBase
{
    public Task23Delivery() : base(23, PointValue.Two) { }

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
