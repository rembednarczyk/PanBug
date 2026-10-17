using System.Text.Json;
using Microsoft.JSInterop;

namespace PanBug.Web.Services;

/// <summary>Zapis jednej zakończonej gry.</summary>
public sealed record GameRecord(int Points, int MaxPoints, int CompletedTasks, long DurationMs, DateTime FinishedAtUtc)
{
    /// <summary>Najdłuższy czas, jaki przyjmujemy z zapisu (dłuższy oznacza uszkodzony wpis).</summary>
    public const long MaxDurationMs = 365L * 24 * 60 * 60 * 1000;

    public TimeSpan Duration => TimeSpan.FromMilliseconds(Math.Clamp(DurationMs, 0, MaxDurationMs));

    /// <summary>Wpis odczytany z localStorage nadaje się do pokazania (nie jest uszkodzony).</summary>
    public bool IsPlausible =>
        Points >= 0 && MaxPoints >= 0 && Points <= MaxPoints && CompletedTasks >= 0 &&
        DurationMs >= 0 && DurationMs <= MaxDurationMs;

    /// <summary>Lepszy wynik: więcej punktów, a przy remisie krótszy czas.</summary>
    public bool IsBetterThan(GameRecord? other) =>
        other is null || Points > other.Points || (Points == other.Points && DurationMs < other.DurationMs);
}

/// <summary>Wyniki gracza przechowywane lokalnie (aplikacja nie ma serwera ani bazy).</summary>
public interface IResultStore
{
    Task<GameRecord?> GetBestAsync();
    Task<IReadOnlyList<GameRecord>> GetHistoryAsync();

    /// <summary>Zapisuje grę w historii; zwraca true, gdy to nowy najlepszy wynik.</summary>
    Task<bool> SaveAsync(GameRecord record);
}

/// <summary>Magazyn wyników w localStorage przeglądarki.</summary>
public sealed class LocalStorageResultStore : IResultStore
{
    public const int HistoryLimit = 20;
    private const string BestKey = "panbug.best.v1";
    private const string HistoryKey = "panbug.history.v1";

    private readonly IJSRuntime _js;

    public LocalStorageResultStore(IJSRuntime js) => _js = js;

    // Wpisy w localStorage może zmienić każdy (DevTools, inna wersja aplikacji), więc odczyt
    // odrzuca uszkodzone rekordy zamiast wywrócić ekran startowy przy każdym uruchomieniu.
    public async Task<GameRecord?> GetBestAsync()
    {
        var best = await ReadAsync<GameRecord>(BestKey);
        if (best is { IsPlausible: true })
            return best;
        // Brak lub uszkodzony najlepszy wynik: odtwarzamy go z historii.
        GameRecord? fromHistory = null;
        foreach (var game in await GetHistoryAsync())
            if (game.IsBetterThan(fromHistory))
                fromHistory = game;
        return fromHistory;
    }

    public async Task<IReadOnlyList<GameRecord>> GetHistoryAsync()
    {
        var history = await ReadAsync<List<GameRecord?>>(HistoryKey);
        return history?.OfType<GameRecord>().Where(g => g.IsPlausible).ToList() ?? new List<GameRecord>();
    }

    public async Task<bool> SaveAsync(GameRecord record)
    {
        var best = await GetBestAsync();
        var history = (await GetHistoryAsync()).Prepend(record).Take(HistoryLimit).ToList();
        if (!await WriteAsync(HistoryKey, history))
            return false; // zapis niemożliwy (np. tryb prywatny): nie ogłaszamy rekordu, którego nie ma

        if (!record.IsBetterThan(best))
            return false;
        return await WriteAsync(BestKey, record);
    }

    private async Task<T?> ReadAsync<T>(string key) where T : class
    {
        var json = await _js.InvokeAsync<string?>("panbug.storage.get", key);
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or ArgumentException)
        {
            return null; // uszkodzony wpis traktujemy jak brak danych
        }
    }

    private async Task<bool> WriteAsync<T>(string key, T value) =>
        await _js.InvokeAsync<bool>("panbug.storage.set", key, JsonSerializer.Serialize(value));
}
