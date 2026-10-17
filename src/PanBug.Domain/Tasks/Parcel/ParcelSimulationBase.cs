using PanBug.Domain.Common;
using PanBug.Domain.Model;
using PanBug.Domain.Tasks.Common;

namespace PanBug.Domain.Tasks.Parcel;

public sealed record ParcelInput(string Height, string Width, string Depth, string Weight, string Phone);

public sealed class SentParcel
{
    public SentParcel(string phone, string code, int locker)
    {
        Phone = phone;
        Code = code;
        Locker = locker;
    }

    public string Phone { get; }
    public string Code { get; }
    public int Locker { get; }
    public bool Collected { get; internal set; }
}

/// <summary>Automat paczkowy: nadanie paczki (wymiary, waga, telefon odbiorcy) i odbiór kodem.</summary>
public abstract class ParcelSimulationBase : TaskBase
{
    public const int MaxHeight = 64;
    public const int MaxWidth = 38;
    public const int MaxDepth = 41;
    public const decimal MinWeight = 0.1m;
    public const decimal MaxWeight = 25m;

    public const string Spec =
        "Automat paczkowy: nadawanie i odbiór paczek.\n\n" +
        "Wymagania:\n" +
        "• Paczka może mieć najwyżej 64 cm wysokości, 38 cm szerokości i 41 cm głębokości. Wartości graniczne są dozwolone, wymiary podaje się w pełnych centymetrach.\n" +
        "• Waga paczki wynosi od 0,1 do 25 kg.\n" +
        "• Numer telefonu odbiorcy można wpisać tak jak w podpowiedzi pola (+48 600 000 000): ze spacjami lub bez, z kierunkowym +48 lub bez niego.\n" +
        "• Po nadaniu odbiorca dostaje 6-cyfrowy kod odbioru. Odbiór wymaga numeru telefonu i tego kodu.";

    // Kolejne kody odbioru (w prawdziwym paczkomacie losowe). Pierwszy zaczyna się od zera.
    private static readonly string[] Codes = { "048213", "731905", "006472", "915338", "072619", "530184" };

    private readonly List<SentParcel> _parcels = new();

    protected ParcelSimulationBase(uint number, PointValue points) : base(number, points, Spec) { }

    public IReadOnlyList<SentParcel> Parcels => _parcels;

    public OperationResult Send(ParcelInput input)
    {
        if (!PolishNumber.TryParseInt(input.Height, out var height) || height < 1 || !HeightAllowed(height))
        {
            if (height == MaxHeight)
                LogDefectVisible(); // wartość graniczna z wymagań odrzucona
            return OperationResult.Fail($"Wysokość musi być liczbą całkowitą od 1 do {MaxHeight} cm.");
        }
        if (!PolishNumber.TryParseInt(input.Width, out var width) || width < 1 || width > MaxWidth)
            return OperationResult.Fail($"Szerokość musi być liczbą całkowitą od 1 do {MaxWidth} cm.");
        if (!PolishNumber.TryParseInt(input.Depth, out var depth) || depth < 1 || depth > MaxDepth)
            return OperationResult.Fail($"Głębokość musi być liczbą całkowitą od 1 do {MaxDepth} cm.");
        if (!PolishNumber.TryParseAmount(input.Weight, out var weight) || weight < MinWeight || weight > MaxWeight)
            return OperationResult.Fail("Waga musi mieścić się w zakresie od 0,1 do 25 kg.");
        if (!TryReadPhone(input.Phone, out var phone))
        {
            if (PhoneNumber.TryNormalize(input.Phone, out _))
                LogDefectVisible(); // numer w formacie z podpowiedzi odrzucony
            return OperationResult.Fail("Numer telefonu odbiorcy musi mieć 9 cyfr, np. +48 600 000 000.");
        }

        var code = Codes[_parcels.Count % Codes.Length];
        var parcel = new SentParcel(phone, code, 7 + _parcels.Count);
        _parcels.Add(parcel);
        return new OperationResult(true,
            $"Paczka nadana do skrytki nr {parcel.Locker}. Kod odbioru: {code}.");
    }

    public OperationResult Collect(string phoneText, string codeText)
    {
        if (!PhoneNumber.TryNormalize(phoneText, out var phone))
            return OperationResult.Fail("Podaj numer telefonu odbiorcy (9 cyfr).");
        var code = (codeText ?? string.Empty).Trim();
        if (code.Length != 6 || !code.All(char.IsAsciiDigit))
            return OperationResult.Fail("Kod odbioru składa się z 6 cyfr.");

        var parcel = _parcels.FirstOrDefault(p => !p.Collected && p.Phone == phone && CodesMatch(p.Code, code));
        if (parcel is null)
        {
            if (_parcels.Any(p => !p.Collected && p.Phone == phone && p.Code == code))
                LogDefectVisible(); // poprawny kod odrzucony
            return OperationResult.Fail("Nie znaleziono paczki dla tego numeru telefonu i kodu.");
        }

        parcel.Collected = true;
        return new OperationResult(true, $"Skrytka nr {parcel.Locker} otwarta. Paczka odebrana.");
    }

    protected virtual bool HeightAllowed(int height) => height <= MaxHeight;

    protected virtual bool TryReadPhone(string? text, out string digits) =>
        PhoneNumber.TryNormalize(text, out digits);

    protected virtual bool CodesMatch(string stored, string entered) => stored == entered;

    protected override void OnReset() => _parcels.Clear();
}
