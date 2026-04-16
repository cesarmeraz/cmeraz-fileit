using System.Threading.Tasks;
using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Module.SimpleFlow.App;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace FileIt.Module.SimpleFlow.Test;

public class WatchInboundTest
{
    [Test]
    public async Task TestRunAsync()
    {
        const string blobName = "incoming-file.txt";
        const string correlationId = "correlation-123";

        var repository = new MockRepository();
        var requestLogRepoMock = repository.Of<ISimpleRequestLogRepo>();
        var blobToolMock = repository.Of<IHandleFiles>();
        var busToolMock = repository.Of<ITalkToApi>();

        var config = new SimpleConfig
        {
            SourceContainer = "source-container",
            WorkingContainer = "working-container",
            ApiAddTopicName = "api-add-topic",
            ApiAddQueueName = "api-add-queue",
        };

        var expectedLog = new SimpleRequestLog
        {
            Id = 1,
            BlobName = blobName,
            ClientRequestId = correlationId,
        };

        ApiRequest? sentMessage = null;

        requestLogRepoMock.AddAsync(blobName, correlationId).Returns(expectedLog);

        blobToolMock.MoveAsync(blobName, config.SourceContainer, config.WorkingContainer).Returns();

        busToolMock
            .SendMessageAsync(Arg.Any<ApiRequest>())
            .Callback(message => sentMessage = message)
            .Returns();

        var watchInbound = new WatchInbound(
            NullLogger<WatchInbound>.Instance,
            blobToolMock.Object,
            busToolMock.Object,
            requestLogRepoMock.Object,
            config
        );

        await watchInbound.RunAsync(blobName, correlationId);

        requestLogRepoMock.VerifyAll();
        blobToolMock.VerifyAll();
        busToolMock.VerifyAll();

        await Assert.That(sentMessage).IsNotNull();
        await Assert.That(sentMessage!.CorrelationId).IsEqualTo(correlationId);
        await Assert.That(sentMessage.QueueName).IsEqualTo(config.ApiAddQueueName);
        await Assert.That(sentMessage.ReplyTo).IsEqualTo(config.ApiAddTopicName);

        var payload = sentMessage.Body as ApiAddPayload;
        await Assert.That(payload).IsNotNull();
        await Assert.That(payload!.FileName).IsEqualTo(blobName);
    }
}
