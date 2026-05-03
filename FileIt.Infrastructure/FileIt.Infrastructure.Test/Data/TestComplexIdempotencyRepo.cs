using FileIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Data;

[TestClass]
public class TestComplexIdempotencyRepo
{
    public required CommonDbContext _db;
    public required Mock<ILogger<ComplexIdempotencyRepo>> _loggerMock;
    public required ComplexIdempotencyRepo target;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CommonDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new CommonDbContext(options);
        _loggerMock = new Mock<ILogger<ComplexIdempotencyRepo>>();
        target = new ComplexIdempotencyRepo(_db, _loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [TestMethod]
    public async Task TryGetAsync_Existing_ReturnsHit()
    {
        await target.SaveAsync("k1", "hash1", 201, "{\"id\":1}", "/api/x/1");

        var hit = await target.TryGetAsync("k1");

        Assert.IsNotNull(hit);
        Assert.AreEqual("k1", hit!.Key);
        Assert.AreEqual("hash1", hit.RequestHash);
        Assert.AreEqual(201, hit.ResponseStatusCode);
        Assert.AreEqual("{\"id\":1}", hit.ResponseBody);
        Assert.AreEqual("/api/x/1", hit.ResponseLocation);
    }

    [TestMethod]
    public async Task TryGetAsync_Missing_ReturnsNull()
    {
        var hit = await target.TryGetAsync("nonexistent");

        Assert.IsNull(hit);
    }

    [TestMethod]
    public async Task SaveAsync_PersistsAllFields()
    {
        await target.SaveAsync("k2", "hash2", 422, null, null);

        var hit = await target.TryGetAsync("k2");
        Assert.IsNotNull(hit);
        Assert.AreEqual(422, hit!.ResponseStatusCode);
        Assert.IsNull(hit.ResponseBody);
        Assert.IsNull(hit.ResponseLocation);
    }

    [TestMethod]
    public async Task SaveAsync_StampsCreatedUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        await target.SaveAsync("k3", "hash3", 200, null, null);

        var hit = await target.TryGetAsync("k3");
        Assert.IsNotNull(hit);
        Assert.IsTrue(hit!.CreatedUtc >= before);
    }

    [TestMethod]
    public async Task TryGetAsync_FindsCorrectKeyAmongMultiple()
    {
        await target.SaveAsync("alpha", "ha", 200, null, null);
        await target.SaveAsync("beta", "hb", 200, null, null);
        await target.SaveAsync("gamma", "hg", 200, null, null);

        var hit = await target.TryGetAsync("beta");

        Assert.IsNotNull(hit);
        Assert.AreEqual("hb", hit!.RequestHash);
    }

    [TestMethod]
    public async Task SaveAsync_CancellationRequested_PropagatesOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.SaveAsync("k", "h", 200, null, null, cts.Token));
    }
}
