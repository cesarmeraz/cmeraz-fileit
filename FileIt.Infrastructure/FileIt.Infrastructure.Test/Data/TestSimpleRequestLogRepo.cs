using FileIt.Domain.Entities;
using FileIt.Infrastructure.Data;
using FileIt.Infrastructure.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Data;

[TestClass]
public class TestSimpleRequestLogRepo
{
    public required InMemoryCommonDbContextFactory _factory;
    public required Mock<ICommonLogConfig> _configMock;
    public required SimpleRequestLogRepo target;

    [TestInitialize]
    public void Setup()
    {
        _factory = new InMemoryCommonDbContextFactory();
        _configMock = new Mock<ICommonLogConfig>();
        _configMock.SetupGet(c => c.Environment).Returns("DEV");
        _configMock.SetupGet(c => c.Host).Returns("test-host");
        _configMock.SetupGet(c => c.Agent).Returns("test-agent");
        target = new SimpleRequestLogRepo(_factory, _configMock.Object);
    }

    [TestCleanup]
    public void Cleanup() => _factory.Dispose();

    [TestMethod]
    public async Task AddAsync_HappyPath_PersistsAllFields()
    {
        var result = await target.AddAsync("blob.csv", "corr-1");

        Assert.IsNotNull(result);
        Assert.AreNotEqual(0, result!.Id);
        Assert.AreEqual("blob.csv", result.BlobName);
        Assert.AreEqual("corr-1", result.ClientRequestId);
        Assert.AreEqual("DEV", result.Environment);
        Assert.AreEqual("test-host", result.Host);
        Assert.AreEqual("test-agent", result.Agent);
        Assert.AreEqual("New", result.Status);
        Assert.AreEqual(0, result.ApiId);
    }

    [TestMethod]
    public async Task AddAsync_NullEnvironmentInConfig_DefaultsToEmpty()
    {
        _configMock.SetupGet(c => c.Environment).Returns((string?)null);

        var result = await target.AddAsync("blob.csv", "corr-1");

        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result!.Environment);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_MatchingEnvironmentAndId_ReturnsRow()
    {
        await target.AddAsync("blob.csv", "corr-X");

        var found = await target.GetByClientRequestIdAsync("corr-X");

        Assert.IsNotNull(found);
        Assert.AreEqual("blob.csv", found!.BlobName);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_DifferentEnvironment_ReturnsNull()
    {
        // Add with DEV environment
        await target.AddAsync("blob.csv", "corr-X");

        // Switch config to PROD; same correlation id should not match
        _configMock.SetupGet(c => c.Environment).Returns("PROD");

        var found = await target.GetByClientRequestIdAsync("corr-X");

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_NullCorrelationId_ReturnsNull()
    {
        await target.AddAsync("blob.csv", "corr-X");

        var found = await target.GetByClientRequestIdAsync(null);

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_FindsCorrectAmongMultiple()
    {
        await target.AddAsync("a.csv", "corr-A");
        await target.AddAsync("b.csv", "corr-B");
        await target.AddAsync("c.csv", "corr-C");

        var found = await target.GetByClientRequestIdAsync("corr-B");

        Assert.IsNotNull(found);
        Assert.AreEqual("b.csv", found!.BlobName);
    }
}
