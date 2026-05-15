using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Module.SimpleFlow.App;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Module.SimpleFlow.Test.WatchInbound;

[TestClass]
public class TestWatchInbound
{
    public required Mock<ILogger<App.WatchInbound>> _loggerMock;
    public required Mock<IHandleFiles> _blobToolMock;
    public required Mock<ITalkToApi> _busToolMock;
    public required Mock<ISimpleRequestLogRepo> _requestLogRepoMock;
    public required SimpleConfig _config;
    public required App.WatchInbound target;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<App.WatchInbound>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );
        _blobToolMock = new Mock<IHandleFiles>();
        _busToolMock = new Mock<ITalkToApi>();
        _requestLogRepoMock = new Mock<ISimpleRequestLogRepo>();

        _config = new SimpleConfig()
        {
            SourceContainer = "simple-source",
            WorkingContainer = "simple-working",
            FinalContainer = "simple-final",
            ApiAddQueueName = "api-add",
            ApiAddTopicName = "api-add-topic",
        };

        target = new App.WatchInbound(
            _loggerMock.Object,
            _blobToolMock.Object,
            _busToolMock.Object,
            _requestLogRepoMock.Object,
            _config
        );
    }

    [TestMethod]
    public async Task RunAsync_HappyPath_LogsThenMovesThenSends()
    {
        var correlationId = Guid.NewGuid().ToString();
        var blobName = "incoming.txt";

        await target.RunAsync(blobName, correlationId);

        _requestLogRepoMock.Verify(x => x.AddAsync(blobName, correlationId), Times.Once);
        _blobToolMock.Verify(x => x.MoveAsync(
            blobName, "simple-source", "simple-working", It.IsAny<CancellationToken>()
        ), Times.Once);
        _busToolMock.Verify(x => x.SendMessageAsync(
            It.Is<ApiRequest>(r =>
                r.CorrelationId == correlationId
                && r.QueueName == "api-add"
                && r.ReplyTo == "api-add-topic"
                && ((ApiAddPayload)r.Body!).FileName == blobName),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [TestMethod]
    public async Task RunAsync_CancellationAfterAdd_ThrowsBeforeMove()
    {
        var correlationId = Guid.NewGuid().ToString();
        var blobName = "incoming.txt";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.RunAsync(blobName, correlationId, cts.Token));

        // Add was called BEFORE the cancellation check, but Move and Send were not
        _requestLogRepoMock.Verify(x => x.AddAsync(blobName, correlationId), Times.Once);
        _blobToolMock.Verify(x => x.MoveAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()
        ), Times.Never);
        _busToolMock.Verify(x => x.SendMessageAsync(
            It.IsAny<ApiRequest>(), It.IsAny<CancellationToken>()
        ), Times.Never);
    }

    [TestMethod]
    public async Task RunAsync_GeneratesUniqueMessageIdPerCall()
    {
        var capturedIds = new List<string>();
        _busToolMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ApiRequest, CancellationToken>((req, _) => capturedIds.Add(req.MessageId))
            .Returns(Task.CompletedTask);

        await target.RunAsync("a.txt", "corr-1");
        await target.RunAsync("b.txt", "corr-2");

        Assert.AreEqual(2, capturedIds.Count);
        Assert.AreNotEqual(capturedIds[0], capturedIds[1], "Each call must produce a unique MessageId");
        Assert.IsTrue(Guid.TryParse(capturedIds[0], out _));
        Assert.IsTrue(Guid.TryParse(capturedIds[1], out _));
    }

    [TestMethod]
    public async Task RunAsync_PayloadFileNameMatchesBlobName()
    {
        ApiAddPayload? capturedPayload = null;
        _busToolMock
            .Setup(x => x.SendMessageAsync(It.IsAny<ApiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ApiRequest, CancellationToken>((req, _) => capturedPayload = req.Body as ApiAddPayload)
            .Returns(Task.CompletedTask);

        await target.RunAsync("specific.csv", Guid.NewGuid().ToString());

        Assert.IsNotNull(capturedPayload);
        Assert.AreEqual("specific.csv", capturedPayload!.FileName);
    }
}
