using PanBug.Web.Services;
using Xunit;

namespace PanBug.Web.Tests;

public class ResultStoreTests
{
    private readonly FakeJsRuntime _js = new();

    private static GameRecord Game(int points, long durationMs) =>
        new(points, 44, 24, durationMs, DateTime.UtcNow);

    [Fact]
    public async Task Empty_storage_has_no_results()
    {
        var store = new LocalStorageResultStore(_js);
        Assert.Null(await store.GetBestAsync());
        Assert.Empty(await store.GetHistoryAsync());
    }

    [Fact]
    public async Task Better_score_or_same_score_faster_becomes_best()
    {
        var store = new LocalStorageResultStore(_js);

        Assert.True(await store.SaveAsync(Game(10, 60_000)));
        Assert.False(await store.SaveAsync(Game(8, 30_000)));   // mniej punktów
        Assert.True(await store.SaveAsync(Game(10, 50_000)));   // remis, szybciej
        Assert.False(await store.SaveAsync(Game(10, 55_000)));  // remis, wolniej

        var best = await store.GetBestAsync();
        Assert.Equal(10, best!.Points);
        Assert.Equal(50_000, best.DurationMs);
        Assert.Equal(4, (await store.GetHistoryAsync()).Count);
    }

    [Fact]
    public async Task History_is_newest_first_and_limited()
    {
        var store = new LocalStorageResultStore(_js);
        for (int i = 1; i <= LocalStorageResultStore.HistoryLimit + 5; i++)
            await store.SaveAsync(Game(i, 1000));

        var history = await store.GetHistoryAsync();
        Assert.Equal(LocalStorageResultStore.HistoryLimit, history.Count);
        Assert.Equal(LocalStorageResultStore.HistoryLimit + 5, history[0].Points);
    }

    [Fact]
    public async Task Corrupted_entry_is_ignored()
    {
        _js.Storage["panbug.best.v1"] = "{to nie json";
        var store = new LocalStorageResultStore(_js);
        Assert.Null(await store.GetBestAsync());
    }

    [Theory]
    [InlineData("[null]")]
    [InlineData("[{\"Points\":5,\"MaxPoints\":44,\"CompletedTasks\":3,\"DurationMs\":9000000000000000000,\"FinishedAtUtc\":\"2026-01-01T00:00:00Z\"}]")]
    [InlineData("[{\"Points\":-3,\"MaxPoints\":44,\"CompletedTasks\":3,\"DurationMs\":1000,\"FinishedAtUtc\":\"2026-01-01T00:00:00Z\"}]")]
    public async Task Corrupted_history_entries_are_dropped_instead_of_crashing(string json)
    {
        _js.Storage["panbug.history.v1"] = json;
        var store = new LocalStorageResultStore(_js);

        var history = await store.GetHistoryAsync();

        Assert.Empty(history);
        Assert.Null(await store.GetBestAsync());
    }

    [Fact]
    public async Task Best_is_rebuilt_from_history_when_missing()
    {
        var store = new LocalStorageResultStore(_js);
        await store.SaveAsync(Game(8, 1000));
        await store.SaveAsync(Game(12, 5000));
        _js.Storage.Remove("panbug.best.v1");

        Assert.Equal(12, (await store.GetBestAsync())!.Points);
    }

    [Fact]
    public async Task Nothing_written_means_no_new_record()
    {
        _js.Writable = false; // np. tryb prywatny
        var store = new LocalStorageResultStore(_js);

        Assert.False(await store.SaveAsync(Game(10, 1000)));
    }

    [Fact]
    public void Duration_of_a_corrupted_record_never_throws() =>
        Assert.Equal(TimeSpan.Zero, new GameRecord(1, 44, 1, -5, DateTime.UtcNow).Duration);

    [Fact]
    public async Task Results_survive_new_store_instance()
    {
        await new LocalStorageResultStore(_js).SaveAsync(Game(12, 1000));
        Assert.Equal(12, (await new LocalStorageResultStore(_js).GetBestAsync())!.Points);
    }
}
