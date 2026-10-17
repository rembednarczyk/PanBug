using System.Text.RegularExpressions;
using PanBug.Domain.Common;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Delivery;

public enum DeliveryField { FirstName, LastName, Street, PostalCode, City, Phone, Notes }

public sealed record DeliveryOrder(
    string FirstName, string LastName, string Street, string PostalCode, string City, string Phone, string Notes);

/// <summary>Formularz zamówienia z dostawą: walidacja pól, opisy błędów, etykiety, uwagi z licznikiem.</summary>
public abstract class DeliverySimulationBase : TaskBase
{
    public const int NotesMaxLength = 500;

    public const string Spec =
        "Formularz zamówienia z dostawą.\n\n" +
        "Wymagania:\n" +
        "• Wszystkie pola poza „Uwagami” są wymagane. Kod pocztowy ma format 00-000, telefon to 9 cyfr (z +48 lub bez, ze spacjami lub bez).\n" +
        "• Formularz spełnia wymagania dostępności WCAG: każdy błąd jest opisany tekstem przy polu, a kliknięcie etykiety przenosi kursor do jej pola.\n" +
        "• Pole „Uwagi” przyjmuje do 500 znaków, a licznik pokazuje, ile znaków zostało. Zamówienie zapisuje uwagi w całości.";

    private static readonly Regex PostalCodePattern = new("^[0-9]{2}-[0-9]{3}\\z", RegexOptions.CultureInvariant);

    private readonly Dictionary<DeliveryField, string> _errors = new();

    protected DeliverySimulationBase(uint number, PointValue points) : base(number, points, Spec) { }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public IReadOnlyDictionary<DeliveryField, string> Errors => _errors;
    public DeliveryOrder? Order { get; private set; }

    /// <summary>Czy błąd pola jest opisany tekstem (i powiązany z polem dla czytnika ekranu).</summary>
    public virtual bool DescribesErrorsWithText => true;

    public static string FieldId(DeliveryField field) => "delivery-" + field.ToString().ToLowerInvariant();

    /// <summary>Identyfikator pola, do którego prowadzi etykieta (atrybut for).</summary>
    public virtual string LabelTarget(DeliveryField field) => FieldId(field);

    public void LabelClicked(DeliveryField field)
    {
        if (LabelTarget(field) != FieldId(field))
            LogDefectVisible(); // kursor trafia do innego pola niż etykieta
    }

    public OperationResult Submit()
    {
        _errors.Clear();
        Require(DeliveryField.FirstName, FirstName, "Podaj imię.");
        Require(DeliveryField.LastName, LastName, "Podaj nazwisko.");
        Require(DeliveryField.Street, Street, "Podaj ulicę i numer domu.");
        if (!PostalCodePattern.IsMatch(PostalCode.Trim()))
            _errors[DeliveryField.PostalCode] = "Kod pocztowy ma format 00-000.";
        Require(DeliveryField.City, City, "Podaj miejscowość.");
        if (!PhoneNumber.TryNormalize(Phone, out var phone))
            _errors[DeliveryField.Phone] = "Numer telefonu musi mieć 9 cyfr, np. +48 600 000 000.";
        if (Notes.Length > NotesMaxLength)
            _errors[DeliveryField.Notes] = $"Uwagi mogą mieć najwyżej {NotesMaxLength} znaków.";

        if (_errors.Count > 0)
        {
            if (!DescribesErrorsWithText)
                LogDefectVisible(); // błąd pokazany wyłącznie kolorem
            return OperationResult.Fail("Popraw zaznaczone pola formularza.");
        }

        var notes = StoreNotes(Notes);
        Order = new DeliveryOrder(FirstName.Trim(), LastName.Trim(), Street.Trim(), PostalCode.Trim(),
            City.Trim(), PhoneNumber.Format(phone), notes);
        if (notes != Notes)
            LogDefectVisible(); // zapisane uwagi różnią się od wpisanych
        return new OperationResult(true, "Zamówienie przyjęte do realizacji.");
    }

    public void NewOrder()
    {
        Order = null;
        _errors.Clear();
        FirstName = LastName = Street = PostalCode = City = Phone = Notes = string.Empty;
    }

    protected virtual string StoreNotes(string notes) => notes;

    private void Require(DeliveryField field, string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            _errors[field] = message;
    }

    protected override void OnReset() => NewOrder();
}
