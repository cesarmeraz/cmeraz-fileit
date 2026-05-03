using FileIt.Domain.Entities.Api;
using FileIt.Infrastructure.Data;

namespace FileIt.Infrastructure.Test.Data;

[TestClass]
public class TestApiLogRepo
{
    public required InMemoryCommonDbContextFactory _factory;
    public required ApiLogRepo target;

    [TestInitialize]
    public void Setup()
    {
        _factory = new InMemoryCommonDbContextFactory();
        target = new ApiLogRepo(_factory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _factory.Dispose();
    }

    [TestMethod]
    public async Task AddAsync_HappyPath_PersistsAllFields()
    {
        var result = await target.AddAsync(
            clientRequestId: "corr-1",
            requestBody: "{\"x\":1}",
            responseBody: "{\"y\":2}",
            status: "Complete");

        Assert.IsNotNull(result);
        Assert.AreNotEqual(0, result!.Id);
        Assert.AreEqual("corr-1", result.ClientRequestId);
        Assert.AreEqual("{\"x\":1}", result.RequestBody);
        Assert.AreEqual("{\"y\":2}", result.ResponseBody);
        Assert.AreEqual("Complete", result.Status);
    }

    [TestMethod]
    public async Task AddAsync_StampsCreatedOnAndModifiedOn()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await target.AddAsync("corr-1", "req", "resp", "Ok");

        Assert.IsNotNull(result);
        Assert.IsTrue(result!.CreatedOn >= before);
        Assert.IsTrue(result.ModifiedOn >= before);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_ExistingRequest_ReturnsRow()
    {
        await target.AddAsync("corr-X", "req", "resp", "Ok");

        var found = await target.GetByClientRequestIdAsync("corr-X");

        Assert.IsNotNull(found);
        Assert.AreEqual("corr-X", found!.ClientRequestId);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_MissingRequest_ReturnsNull()
    {
        var found = await target.GetByClientRequestIdAsync("nonexistent");

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task GetByClientRequestIdAsync_FindsCorrectRowAmongMultiple()
    {
        await target.AddAsync("corr-A", "ra", "respA", "Ok");
        await target.AddAsync("corr-B", "rb", "respB", "Ok");
        await target.AddAsync("corr-C", "rc", "respC", "Ok");

        var found = await target.GetByClientRequestIdAsync("corr-B");

        Assert.IsNotNull(found);
        Assert.AreEqual("rb", found!.RequestBody);
    }

    [TestMethod]
    public async Task GetByIdAsync_ExistingRow_ReturnsIt()
    {
        var added = await target.AddAsync("corr-1", "req", "resp", "Ok");

        var found = await target.GetByIdAsync(added!.Id);

        Assert.IsNotNull(found);
        Assert.AreEqual(added.Id, found!.Id);
    }

    [TestMethod]
    public async Task GetByIdAsync_MissingRow_ReturnsNull()
    {
        var found = await target.GetByIdAsync(999999);

        Assert.IsNull(found);
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsAllRows()
    {
        await target.AddAsync("a", "ra", "respA", "Ok");
        await target.AddAsync("b", "rb", "respB", "Ok");
        await target.AddAsync("c", "rc", "respC", "Ok");

        var all = await target.GetAllAsync();

        Assert.AreEqual(3, all.Count());
    }
}
