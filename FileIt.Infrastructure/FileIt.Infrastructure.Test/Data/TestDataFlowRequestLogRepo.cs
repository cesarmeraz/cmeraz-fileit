using FileIt.Domain.Entities;
using FileIt.Infrastructure.Data;
using FileIt.Infrastructure.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Data;

[TestClass]
public class TestDataFlowRequestLogRepo
{
    public required InMemoryCommonDbContextFactory _factory;
    public required Mock<ICommonLogConfig> _configMock;
    public required DataFlowRequestLogRepo target;

    [TestInitialize]
    public void Setup()
    {
        _factory = new InMemoryCommonDbContextFactory();
        _configMock = new Mock<ICommonLogConfig>();
        _configMock.SetupGet(c => c.Environment).Returns("DEV");
        _configMock.SetupGet(c => c.Host).Returns("test-host");
        _configMock.SetupGet(c => c.Agent).Returns("test-agent");
        target = new DataFlowRequestLogRepo(_factory, _configMock.Object);
    }

    [TestCleanup]
    public void Cleanup() => _factory.Dispose();

    [TestMethod]
    public async Task AddAsync_HappyPath_PersistsAllFields()
    {
        var result = await target.AddAsync("gl.csv", "corr-1");

        Assert.IsNotNull(result);
        Assert.AreNotEqual(0, result!.Id);
        Assert.AreEqual("gl.csv", result.BlobName);
        Assert.AreEqual("corr-1", result.ClientRequestId);
        Assert.AreEqual("DEV", result.Environment);
        Assert.AreEqual("New", result.Status);
        Assert.AreEqual(0, result.RowsIngested);
        Assert.AreEqual(0, result.RowsTransformed);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_Existing_ReturnsRow()
    {
        await target.AddAsync("gl.csv", "corr-X");

        var found = await target.GetByClientRequestIdAsync("corr-X");

        Assert.IsNotNull(found);
        Assert.AreEqual("gl.csv", found!.BlobName);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_DifferentEnvironment_ReturnsNull()
    {
        await target.AddAsync("gl.csv", "corr-X");
        _configMock.SetupGet(c => c.Environment).Returns("UAT");

        var found = await target.GetByClientRequestIdAsync("corr-X");

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task UpdateTransformResultAsync_Existing_UpdatesAllFields()
    {
        await target.AddAsync("gl.csv", "corr-X");

        await target.UpdateTransformResultAsync("corr-X", 24, "summary_gl.csv", "Complete");

        var updated = await target.GetByClientRequestIdAsync("corr-X");
        Assert.IsNotNull(updated);
        Assert.AreEqual(24, updated!.RowsTransformed);
        Assert.AreEqual("summary_gl.csv", updated.ExportBlobName);
        Assert.AreEqual("Complete", updated.Status);
    }

    [TestMethod]
    public async Task UpdateTransformResultAsync_Missing_DoesNothing()
    {
        // No exception, no row inserted, no error log.
        await target.UpdateTransformResultAsync("nonexistent", 5, "x", "Complete");

        var found = await target.GetByClientRequestIdAsync("nonexistent");
        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task UpdateTransformResultAsync_StampsModifiedOn()
    {
        await target.AddAsync("gl.csv", "corr-X");
        var before = DateTime.UtcNow.AddSeconds(-1);
        // Sleep briefly so the ModifiedOn after the update is observably later.
        await Task.Delay(20);

        await target.UpdateTransformResultAsync("corr-X", 1, "x", "Ok");

        var updated = await target.GetByClientRequestIdAsync("corr-X");
        Assert.IsNotNull(updated);
        Assert.IsTrue(updated!.ModifiedOn >= before);
    }
}
