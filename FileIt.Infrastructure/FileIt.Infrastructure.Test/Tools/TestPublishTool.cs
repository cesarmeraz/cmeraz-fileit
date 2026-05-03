using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Tools;

[TestClass]
public class TestPublishTool
{
    public required Mock<ILogger<PublishTool>> _loggerMock;
    public required Mock<ServiceBusSender> _senderMock;
    public required Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
    public required PublishTool target;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<PublishTool>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()));
        _senderMock = new Mock<ServiceBusSender>();
        _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        _senderFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_senderMock.Object);

        target = new PublishTool(_senderFactoryMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task EmitAsync_NullResponse_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => target.EmitAsync(null!));
    }

    [TestMethod]
    public async Task EmitAsync_EmptyTopicName_ThrowsArgumentException()
    {
        var response = new ApiAddResponse()
        {
            NodeId = 1,
            CorrelationId = "corr-1",
            TopicName = "",
            Subject = "api-add-response",
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => target.EmitAsync(response));
    }

    [TestMethod]
    public async Task EmitAsync_HappyPath_SendsMessageWithCorrelationIdAndSubject()
    {
        ServiceBusMessage? captured = null;
        _senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        var response = new ApiAddResponse()
        {
            NodeId = 42,
            CorrelationId = "corr-xyz",
            TopicName = "api-add-topic",
            Subject = "api-add-response",
        };

        await target.EmitAsync(response);

        Assert.IsNotNull(captured);
        Assert.AreEqual("corr-xyz", captured!.CorrelationId);
        Assert.AreEqual("api-add-response", captured.Subject);
        Assert.AreEqual("application/json", captured.ContentType);
    }

    [TestMethod]
    public async Task EmitAsync_StampsEnqueuedTimeUtcApplicationProperty()
    {
        ServiceBusMessage? captured = null;
        _senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);
        var beforeSend = DateTime.UtcNow;

        await target.EmitAsync(new ApiAddResponse()
        {
            NodeId = 1,
            CorrelationId = "c",
            TopicName = "t",
            Subject = "s",
        });

        Assert.IsNotNull(captured);
        Assert.IsTrue(captured!.ApplicationProperties.ContainsKey(FileItMessageProperties.EnqueuedTimeUtc),
            "EnqueuedTimeUtc property must be stamped on every emitted response");
        var stamped = DateTime.Parse(
            (string)captured.ApplicationProperties[FileItMessageProperties.EnqueuedTimeUtc]!,
            null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.IsTrue(stamped >= beforeSend.AddSeconds(-1) && stamped <= DateTime.UtcNow.AddSeconds(1));
    }

    [TestMethod]
    public async Task EmitAsync_SenderFactoryThrows_LogsAndRethrows()
    {
        _senderFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Throws(new InvalidOperationException("no client for topic"));

        var response = new ApiAddResponse()
        {
            NodeId = 1,
            CorrelationId = "c",
            TopicName = "t",
            Subject = "s",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => target.EmitAsync(response));
    }

    [TestMethod]
    public async Task EmitAsync_CancellationRequested_ThrowsBeforeSending()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var response = new ApiAddResponse()
        {
            NodeId = 1,
            CorrelationId = "c",
            TopicName = "t",
            Subject = "s",
        };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.EmitAsync(response, cts.Token));

        _senderMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()
        ), Times.Never);
    }

    [TestMethod]
    public async Task EmitAsync_NullCorrelationId_StillSendsWithEmptyScope()
    {
        ServiceBusMessage? captured = null;
        _senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        var response = new ApiAddResponse()
        {
            NodeId = 1,
            CorrelationId = null,
            TopicName = "t",
            Subject = "s",
        };

        await target.EmitAsync(response);

        Assert.IsNotNull(captured);
        // CorrelationId on ServiceBusMessage is null when source is null
        Assert.IsNull(captured!.CorrelationId);
    }
}
