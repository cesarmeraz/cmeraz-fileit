using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Data;
using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.Middleware;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace FileIt.Infrastructure.Integration;

[TestClass]
public class ApiLogRepoTest : BaseTest
{
    [TestMethod]
    public async Task TestAdd()
    {
        using var host = TestHost.CreateHost();
        string clientRequestId = Guid.NewGuid().ToString();
        string requestBody = "test body";
        string responseBody = "test response";
        string status = "Integration test";
        var target = host.Services.GetService<IApiLogRepo>();
        Assert.IsNotNull(target, $"Failed to create {nameof(IApiLogRepo)}");

        var log = await target.AddAsync(clientRequestId, requestBody, responseBody, status);
        Assert.IsNotNull(log, $"Failed to create {nameof(ApiLog)} for log");
        Assert.AreEqual(requestBody, log.RequestBody);
        Assert.AreEqual(clientRequestId, log.ClientRequestId);

        var logGetById = await target.GetByIdAsync(log.Id);
        Assert.IsNotNull(logGetById, $"Failed to get {nameof(ApiLog)} for logGetById");

        var logGetByClientRequestId = await target.GetByClientRequestIdAsync(clientRequestId);
        Assert.IsNotNull(
            logGetByClientRequestId,
            $"Failed to get {nameof(ApiLog)} for logGetByClientRequestId"
        );

        string logGetByIdJson = JsonSerializer.Serialize(logGetById);
        Debug.WriteLine(logGetByIdJson);
        string logGetByClientRequestIdJson = JsonSerializer.Serialize(logGetByClientRequestId);
        Debug.WriteLine(logGetByClientRequestIdJson);
        Assert.AreEqual(log.RequestBody, logGetById.RequestBody);
        Assert.AreEqual(logGetByIdJson, logGetByClientRequestIdJson);
    }

    [TestMethod]
    public async Task TestUpdate()
    {
        using var host = TestHost.CreateHost();
        string requestBody = "test body";
        string responseBody = "test response";
        string status = "Integration test";
        string clientRequestId = Guid.NewGuid().ToString();
        var target = host.Services.GetService<IApiLogRepo>();
        Assert.IsNotNull(target, $"Failed to create {nameof(IApiLogRepo)}");

        var log = await target.AddAsync(clientRequestId, requestBody, responseBody, status);
        Assert.IsNotNull(log, $"Failed to create {nameof(ApiLog)} for log");
        Assert.AreEqual(requestBody, log.RequestBody);
        Assert.AreEqual(clientRequestId, log.ClientRequestId);

        var foundLog = await target.GetByIdAsync(log.Id);
        Assert.IsNotNull(foundLog, $"Failed to create {nameof(ApiLog)} for foundLog");
        foundLog.RequestBody = "test RequestBody after update";

        var updatedLog = await target.UpdateAsync(foundLog);
        Assert.IsNotNull(updatedLog, $"Failed to create {nameof(ApiLog)} for updatedLog");
        Assert.AreEqual(foundLog.RequestBody, updatedLog.RequestBody);
        Assert.AreNotEqual(log.RequestBody, updatedLog.RequestBody);
        Assert.AreEqual(log.ClientRequestId, updatedLog.ClientRequestId);
        Assert.IsTrue(log.ModifiedOn < updatedLog.ModifiedOn);
    }
}
