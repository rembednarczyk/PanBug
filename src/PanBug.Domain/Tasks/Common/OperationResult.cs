namespace PanBug.Domain.Tasks.Common;

/// <summary>Wynik operacji symulacji: sukces albo komunikat błędu pokazywany graczowi pod formularzem.</summary>
public sealed record OperationResult(bool Success, string? Message = null)
{
    public static readonly OperationResult Ok = new(true);
    public static OperationResult Fail(string message) => new(false, message);
}
