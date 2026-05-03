using FileIt.Domain.Entities;
using FileIt.Infrastructure.Data;
using FileIt.Infrastructure.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Data;

[TestClass]
public class TestCommonLogRepo
{
    public required InMemoryCommonDbContextFactory _factory;
    public required Mock<ICommonLogConfig> _configMock;
    public required CommonLogRepo target;

    [TestInitialize]
    public void Setup()
    {
        _factory = new InMemoryCommonDbContextFactory();
        _configMock = new Mock<ICommonLogConfig>();
        _configMock.SetupGet(c => c.Environment).Returns("DEV");
        target = new CommonLogRepo(_factory, _configMock.Object);
    }

    [TestCleanup]
    public void Cleanup() => _factory.Dispose();

    private static CommonLog BuildLog(
        string? correlationId = "corr-1",
        string env = "DEV",
        string message = "test")
    {
        return new CommonLog
        {
            CorrelationId = correlationId,
            Environment = env,
            Message = message,
            CreatedOn = DateTime.UtcNow,
            ModifiedOn = DateTime.UtcNow,
            Level = "Information",
        };
    }

    [TestMethod]
    public async Task AddAsync_ViaBaseRepo_PersistsLog()
    {
        var log = BuildLog();
        var result = await target.AddAsync(log);

        Assert.IsNotNull(result);
        Assert.AreNotEqual(0, result!.Id);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_MatchingEnvAndId_ReturnsLog()
    {
        await target.AddAsync(BuildLog(correlationId: "corr-X", env: "DEV"));

        var found = await target.GetByClientRequestIdAsync("corr-X");

        Assert.IsNotNull(found);
        Assert.AreEqual("corr-X", found!.CorrelationId);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_DifferentEnvironment_ReturnsNull()
    {
        await target.AddAsync(BuildLog(correlationId: "corr-X", env: "DEV"));
        _configMock.SetupGet(c => c.Environment).Returns("PROD");

        var found = await target.GetByClientRequestIdAsync("corr-X");

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_Missing_ReturnsNull()
    {
        var found = await target.GetByClientRequestIdAsync("nonexistent");

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_NullId_ReturnsNullWhenNoMatch()
    {
        // No row with null CorrelationId yet. Lookup returns null.
        var found = await target.GetByClientRequestIdAsync(null);

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_FindsCorrectAmongMultiple()
    {
        await target.AddAsync(BuildLog(correlationId: "alpha"));
        await target.AddAsync(BuildLog(correlationId: "beta"));
        await target.AddAsync(BuildLog(correlationId: "gamma"));

        var found = await target.GetByClientRequestIdAsync("beta");

        Assert.IsNotNull(found);
        Assert.AreEqual("beta", found!.CorrelationId);
    }
}
