using FileIt.Domain.Interfaces;
using FileIt.Module.Complex.App;
using FileIt.Module.Complex.App.Behavior;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileIt.Module.Complex.Test;

[TestClass]
public class IdempotencyManagerTests
{
    private static IdempotencyManager NewManager(IComplexIdempotencyRepo repo, IdempotencyOptions? opts = null)
    {
        var config = new ComplexConfig { Idempotency = opts ?? new IdempotencyOptions() };
        return new IdempotencyManager(config, repo, NullLogger<IdempotencyManager>.Instance);
    }

    [TestMethod]
    public async Task NoKey_Skips()
    {
        var repo = new FakeRepo();
        var sut = NewManager(repo);

        var outcome = await sut.CheckAsync(null, "{\"a\":1}");

        Assert.AreEqual(IdempotencyState.Skip, outcome.State);
    }

    [TestMethod]
    public async Task UnknownKey_ProceedsAndSaves()
    {
        var repo = new FakeRepo();
        var sut = NewManager(repo);

        var outcome = await sut.CheckAsync("alpha-1", "{\"a\":1}");
        Assert.AreEqual(IdempotencyState.Proceed, outcome.State);

        await sut.SaveAsync("alpha-1", "{\"a\":1}", 201, "{\"id\":\"x\"}", "/api/documents/x", default);
        Assert.AreEqual(1, repo.Saved.Count);
        Assert.AreEqual("alpha-1", repo.Saved[0].Key);
    }

    [TestMethod]
    public async Task SameKeySameBody_Replays()
    {
        var repo = new FakeRepo();
        var sut = NewManager(repo);
        var hash = sut.ComputeRequestHash("{\"a\":1}");
        repo.SeedHit(new IdempotencyHit("alpha-1", hash, 201, "{\"id\":\"x\"}", "/api/documents/x", DateTime.UtcNow));

        var outcome = await sut.CheckAsync("alpha-1", "{\"a\":1}");

        Assert.AreEqual(IdempotencyState.Replay, outcome.State);
        Assert.IsNotNull(outcome.Hit);
        Assert.AreEqual(201, outcome.Hit!.ResponseStatusCode);
    }

    [TestMethod]
    public async Task SameKeyDifferentBody_Conflicts()
    {
        var repo = new FakeRepo();
        var sut = NewManager(repo);
        var firstHash = sut.ComputeRequestHash("{\"a\":1}");
        repo.SeedHit(new IdempotencyHit("alpha-1", firstHash, 201, "{}", null, DateTime.UtcNow));

        var outcome = await sut.CheckAsync("alpha-1", "{\"a\":2}");

        Assert.AreEqual(IdempotencyState.Conflict, outcome.State);
        Assert.IsNotNull(outcome.RejectReason);
    }

    [TestMethod]
    public async Task KeyTooLong_Invalid()
    {
        var repo = new FakeRepo();
        var sut = NewManager(repo, new IdempotencyOptions { MaxKeyLength = 5 });

        var outcome = await sut.CheckAsync("too-long-key", "{}");

        Assert.AreEqual(IdempotencyState.Invalid, outcome.State);
    }

    private sealed class FakeRepo : IComplexIdempotencyRepo
    {
        public List<(string Key, string Hash, int Status)> Saved { get; } = new();
        private readonly Dictionary<string, IdempotencyHit> _hits = new();

        public void SeedHit(IdempotencyHit hit) => _hits[hit.Key] = hit;

        public Task<IdempotencyHit?> TryGetAsync(string key, CancellationToken ct = default)
            => Task.FromResult(_hits.TryGetValue(key, out var hit) ? hit : null);

        public Task SaveAsync(string key, string requestHash, int status, string? body, string? loc, CancellationToken ct = default)
        {
            Saved.Add((key, requestHash, status));
            _hits[key] = new IdempotencyHit(key, requestHash, status, body, loc, DateTime.UtcNow);
            return Task.CompletedTask;
        }
    }
}
