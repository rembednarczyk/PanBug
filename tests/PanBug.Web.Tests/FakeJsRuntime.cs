using Microsoft.JSInterop;

namespace PanBug.Web.Tests;

/// <summary>Atrapa IJSRuntime odwzorowująca panbug.storage (localStorage) w słowniku.</summary>
public sealed class FakeJsRuntime : IJSRuntime
{
    public Dictionary<string, string> Storage { get; } = new();

    /// <summary>false udaje localStorage bez możliwości zapisu (tryb prywatny, brak miejsca).</summary>
    public bool Writable { get; set; } = true;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        var key = (string)args![0]!;
        switch (identifier)
        {
            case "panbug.storage.get":
                return ValueTask.FromResult((TValue)(object?)Storage.GetValueOrDefault(key)!);
            case "panbug.storage.set":
                if (Writable)
                    Storage[key] = (string)args[1]!;
                return ValueTask.FromResult((TValue)(object)Writable);
            default:
                throw new InvalidOperationException($"Nieoczekiwane wywołanie JS: {identifier}");
        }
    }
}
