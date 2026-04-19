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

public class ApiLogRepoTest : BaseTest
{
    [Test]
    public async Task TestAdd()
    {
        using var host = TestHost.CreateHost();
        string clientRequestId = Guid.NewGuid().ToString();
        string requestBody = "test body";
        string responseBody = "test response";
        string status = "Integration test";
        var target = host.Services.GetService<IApiLogRepo>();
        await Assert.That(target).IsNotNull();

        var log = await target!.AddAsync(clientRequestId, requestBody, responseBody, status);
        await Assert.That(log).IsNotNull();
        await Assert.That(log!.RequestBody).IsEqualTo(requestBody);
        await Assert.That(log.ClientRequestId).IsEqualTo(clientRequestId);

        var logGetById = await target.GetByIdAsync(log.Id);
        await Assert.That(logGetById).IsNotNull();

        var logGetByClientRequestId = await target.GetByClientRequestIdAsync(clientRequestId);
        await Assert.That(logGetByClientRequestId).IsNotNull();

        string logGetByIdJson = JsonSerializer.Serialize(logGetById);
        Debug.WriteLine(logGetByIdJson);
        string logGetByClientRequestIdJson = JsonSerializer.Serialize(logGetByClientRequestId);
        Debug.WriteLine(logGetByClientRequestIdJson);
        await Assert.That(log.RequestBody).IsEqualTo(logGetById!.RequestBody);
        await Assert.That(logGetByClientRequestIdJson).IsEqualTo(logGetByIdJson);
    }

    [Test]
    public async Task TestUpdate()
    {
        using var host = TestHost.CreateHost();
        string requestBody = "test body";
        string responseBody = "test response";
        string status = "Integration test";
        string clientRequestId = Guid.NewGuid().ToString();
        var target = host.Services.GetService<IApiLogRepo>();
        await Assert.That(target).IsNotNull();

        var log = await target!.AddAsync(clientRequestId, requestBody, responseBody, status);
        await Assert.That(log).IsNotNull();
        await Assert.That(log!.RequestBody).IsEqualTo(requestBody);
        await Assert.That(log.ClientRequestId).IsEqualTo(clientRequestId);

        var foundLog = await target.GetByIdAsync(log.Id);
        await Assert.That(foundLog).IsNotNull();
        foundLog!.RequestBody = "test RequestBody after update";

        var updatedLog = await target.UpdateAsync(foundLog);
        await Assert.That(updatedLog).IsNotNull();
        await Assert.That(updatedLog!.RequestBody).IsEqualTo(foundLog.RequestBody);
        await Assert.That(updatedLog.RequestBody).IsNotEqualTo(log.RequestBody);
        await Assert.That(updatedLog.ClientRequestId).IsEqualTo(log.ClientRequestId);
        await Assert.That(log.ModifiedOn < updatedLog.ModifiedOn).IsTrue();
    }
}
