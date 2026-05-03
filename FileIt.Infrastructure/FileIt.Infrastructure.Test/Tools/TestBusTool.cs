using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Tools;

[TestClass]
public class TestBusTool
{
    public required Mock<ILogger<BusTool>> _loggerMock;
    public required Mock<ServiceBusSender> _senderMock;
    public required Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
    public required BusTool target;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<BusTool>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );
        _senderMock = new Mock<ServiceBusSender>();
        _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        _senderFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_senderMock.Object);

        target = new BusTool(_loggerMock.Object, _senderFactoryMock.Object);
    }

    [TestMethod]
    public async Task SendMessageAsync_EmptyQueueName_ThrowsArgumentException()
    {
        var request = new ApiRequest("test-id") { Body = "x", QueueName = "" };

        await Assert.ThrowsAsync<ArgumentException>(
            () => target.SendMessageAsync(request));
    }

    [TestMethod]
    public async Task SendMessageAsync_HappyPath_BuildsMessageWithRequiredHeaders()
    {
        ServiceBusMessage? captured = null;
        _senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        var request = new ApiRequest("msg-123")
        {
            Body = new { hello = "world" },
            QueueName = "api-add",
            CorrelationId = "corr-1",
            ReplyTo = "api-add-topic",
            Subject = "api-add-simple",
        };

        await target.SendMessageAsync(request);

        Assert.IsNotNull(captured);
        Assert.AreEqual("application/json", captured!.ContentType);
        Assert.AreEqual("corr-1", captured.CorrelationId);
        Assert.AreEqual("msg-123", captured.MessageId);
        Assert.AreEqual("api-add-topic", captured.ReplyTo);
        Assert.AreEqual("api-add-simple", captured.Subject);
    }

    [TestMethod]
    public async Task SendMessageAsync_StampsEnqueuedTimeUtcApplicationProperty()
    {
        ServiceBusMessage? captured = null;
        _senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);
        var beforeSend = DateTime.UtcNow;

        await target.SendMessageAsync(new ApiRequest("m-1") { Body = "x", QueueName = "q" });

        Assert.IsNotNull(captured);
        Assert.IsTrue(captured!.ApplicationProperties.ContainsKey(FileItMessageProperties.EnqueuedTimeUtc),
            "EnqueuedTimeUtc property must be stamped on every outgoing message");
        var stamped = DateTime.Parse((string)captured.ApplicationProperties[FileItMessageProperties.EnqueuedTimeUtc]!,
            null, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.IsTrue(stamped >= beforeSend.AddSeconds(-1) && stamped <= DateTime.UtcNow.AddSeconds(1),
            $"EnqueuedTimeUtc {stamped:O} should be within the test window");
    }

    [TestMethod]
    public async Task SendMessageAsync_NullBody_ProducesMessageWithEmptyBody()
    {
        ServiceBusMessage? captured = null;
        _senderMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await target.SendMessageAsync(new ApiRequest("m-1") { Body = null, QueueName = "q" });

        Assert.IsNotNull(captured);
        // Null body still produces a valid message; headers are still set.
        Assert.AreEqual("application/json", captured!.ContentType);
    }

    [TestMethod]
    public async Task SendMessageAsync_CancellationRequested_ThrowsBeforeSending()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new ApiRequest("m-1") { Body = "x", QueueName = "q" };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.SendMessageAsync(request, cts.Token));

        _senderMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()
        ), Times.Never);
    }
}
