using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanBug.Domain.Scoring;
using PanBug.Web.Services;

namespace PanBug.Web.Tests;

/// <summary>Kontekst bUnit z prawdziwymi serwisami aplikacji i wynikami zapisywanymi w pamięci.</summary>
public abstract class WebTestBase : TestContext
{
    protected WebTestBase()
    {
        var polish = new CultureInfo("pl-PL");
        CultureInfo.CurrentCulture = polish;
        CultureInfo.CurrentUICulture = polish;

        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IScoringEngine>(new StandardScoringEngine());
        Services.AddSingleton<IResultStore>(Results);
        Services.AddSingleton<GameSession>();
    }

    protected InMemoryResultStore Results { get; } = new();

    protected GameSession Session => Services.GetRequiredService<GameSession>();
}

public sealed class InMemoryResultStore : IResultStore
{
    public List<GameRecord> History { get; } = new();
    public GameRecord? Best { get; set; }

    public Task<GameRecord?> GetBestAsync() => Task.FromResult(Best);

    public Task<IReadOnlyList<GameRecord>> GetHistoryAsync() =>
        Task.FromResult<IReadOnlyList<GameRecord>>(History.ToList());

    public Task<bool> SaveAsync(GameRecord record)
    {
        History.Insert(0, record);
        if (!record.IsBetterThan(Best))
            return Task.FromResult(false);
        Best = record;
        return Task.FromResult(true);
    }
}
