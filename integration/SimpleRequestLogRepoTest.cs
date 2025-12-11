using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using FileIt.App.Models;
using FileIt.App.Repositories;
using FileIt.App.Simple;
using Microsoft.Extensions.DependencyInjection;

namespace FileIt.Integration.Test;

[TestClass]
public class SimpleRequestLogRepoTest : BaseTest
{
    [TestMethod]
    public async Task TestAdd()
    {
        using var host = TestHost.CreateHost();
        string blobName = "testblobname";
        string clientRequestId = Guid.NewGuid().ToString();
        var target = host.Services.GetService<ISimpleRequestLogRepo>();
        Assert.IsNotNull(target, $"Failed to create {nameof(SimpleRequestLogRepo)}");

        var log = await target.AddAsync(blobName, clientRequestId);
        Assert.IsNotNull(log, $"Failed to create {nameof(SimpleRequestLog)} for log");
        Assert.AreEqual(blobName, log.BlobName);
        Assert.AreEqual(clientRequestId, log.ClientRequestId);

        var logGetById = await target.GetByIdAsync(log.Id);
        Assert.IsNotNull(logGetById, $"Failed to create {nameof(SimpleRequestLog)} for logGetById");

        var logGetByClientRequestId = await target.GetByClientRequestIdAsync(clientRequestId);
        Assert.IsNotNull(
            logGetByClientRequestId,
            $"Failed to create {nameof(SimpleRequestLog)} for logGetByClientRequestId"
        );

        string logGetByIdJson = JsonSerializer.Serialize(logGetById);
        Debug.WriteLine(logGetByIdJson);
        string logGetByClientRequestIdJson = JsonSerializer.Serialize(logGetByClientRequestId);
        Debug.WriteLine(logGetByClientRequestIdJson);
        Assert.AreEqual(log.BlobName, logGetById.BlobName);
        Assert.AreEqual(logGetByIdJson, logGetByClientRequestIdJson);
    }

    [TestMethod]
    public async Task TestUpdate()
    {
        using var host = TestHost.CreateHost();
        string blobName = "testblobname";
        string clientRequestId = Guid.NewGuid().ToString();
        var target = host.Services.GetService<ISimpleRequestLogRepo>();
        Assert.IsNotNull(target, $"Failed to create {nameof(SimpleRequestLogRepo)}");

        var log = await target.AddAsync(blobName, clientRequestId);
        Assert.IsNotNull(log, $"Failed to create {nameof(SimpleRequestLog)} for log");
        Assert.AreEqual(blobName, log.BlobName);
        Assert.AreEqual(clientRequestId, log.ClientRequestId);

        var foundLog = await target.GetByIdAsync(log.Id);

        log.ApiId = 42;
        var updatedLog = await target.UpdateAsync(log);
        Assert.IsNotNull(updatedLog, $"Failed to create {nameof(SimpleRequestLog)} for updatedLog");
        Assert.AreEqual(log.ApiId, updatedLog.ApiId);
        Assert.IsTrue(log.ModifiedOn < updatedLog.ModifiedOn);
    }
}
