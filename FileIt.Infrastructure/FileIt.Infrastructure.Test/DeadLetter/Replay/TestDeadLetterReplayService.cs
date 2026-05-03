using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.DeadLetter;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.DeadLetter.Replay;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.DeadLetter.Replay;

[TestClass]
public class TestDeadLetterReplayService
{
    public required Mock<IDeadLetterRecordRepo> _repoMock;
    public required Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
    public required Mock<ServiceBusSender> _senderMock;
    public required Mock<ILogger<DeadLetterReplayService>> _loggerMock;
    public required DeadLetterReplayService target;

    [TestInitialize]
    public void Setup()
    {
        _repoMock = new Mock<IDeadLetterRecordRepo>();
        _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        _senderMock = new Mock<ServiceBusSender>();
        _loggerMock = new Mock<ILogger<DeadLetterReplayService>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );
        _senderFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(_senderMock.Object);

        target = new DeadLetterReplayService(
            _repoMock.Object,
            _senderFactoryMock.Object,
            _loggerMock.Object);
    }

    private static DeadLetterRecord BuildRecord(
        long id = 1,
        DeadLetterRecordStatus status = DeadLetterRecordStatus.PendingReplay,
        string sourceEntity = "dataflow-transform",
        string? body = "{\"hello\":\"world\"}",
        string? correlationId = "corr-1",
        int replayAttempts = 0,
        string? contentType = "application/json")
    {
        return new DeadLetterRecord
        {
            DeadLetterRecordId = id,
            MessageId = $"msg-{id}",
            CorrelationId = correlationId,
            Status = status,
            SourceEntityName = sourceEntity,
            SourceEntityType = SourceEntityType.Queue,
            MessageBody = body!,
            ContentType = contentType,
            ReplayAttemptCount = replayAttempts,
            EnqueuedTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            DeadLetteredTimeUtc = DateTime.UtcNow.AddMinutes(-5),
            DeliveryCount = 5,
        };
    }

    // ---- Constructor null guards ----

    [TestMethod]
    public void Constructor_NullRepo_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeadLetterReplayService(null!, _senderFactoryMock.Object, _loggerMock.Object));
    }

    [TestMethod]
    public void Constructor_NullSenderFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeadLetterReplayService(_repoMock.Object, null!, _loggerMock.Object));
    }

    [TestMethod]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeadLetterReplayService(_repoMock.Object, _senderFactoryMock.Object, null!));
    }

    // ---- ReplayAsync: input guards ----

    [TestMethod]
    public async Task ReplayAsync_NullInitiatedBy_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.ReplayAsync(1, null!));
    }

    [TestMethod]
    public async Task ReplayAsync_EmptyInitiatedBy_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.ReplayAsync(1, ""));
    }

    [TestMethod]
    public async Task ReplayAsync_CancellationRequested_ThrowsBeforeWork()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.ReplayAsync(1, "operator", cts.Token));

        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Lifecycle outcomes ----

    [TestMethod]
    public async Task ReplayAsync_RecordNotFound_ReturnsNotFoundNoLifecycleUpdate()
    {
        _repoMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetterRecord?)null);

        var outcome = await target.ReplayAsync(99, "operator");

        Assert.AreEqual(DeadLetterReplayResult.NotFound, outcome.Result);
        Assert.AreEqual(99, outcome.DeadLetterRecordId);
        _repoMock.Verify(r => r.UpdateLifecycleAsync(
            It.IsAny<long>(), It.IsAny<DeadLetterRecordStatus>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ReplayAsync_StatusNotPendingReplay_ReturnsNotEligibleNoLifecycleUpdate()
    {
        var record = BuildRecord(id: 5, status: DeadLetterRecordStatus.UnderReview);
        _repoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var outcome = await target.ReplayAsync(5, "operator");

        Assert.AreEqual(DeadLetterReplayResult.NotEligible, outcome.Result);
        StringAssert.Contains(outcome.Reason, "UnderReview");
        _repoMock.Verify(r => r.UpdateLifecycleAsync(
            It.IsAny<long>(), It.IsAny<DeadLetterRecordStatus>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task ReplayAsync_EmptySourceEntity_PushesToUnderReview()
    {
        var record = BuildRecord(id: 5, sourceEntity: "");
        _repoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var outcome = await target.ReplayAsync(5, "operator");

        Assert.AreEqual(DeadLetterReplayResult.InvalidRecord, outcome.Result);
        _repoMock.Verify(r => r.UpdateLifecycleAsync(
            5,
            DeadLetterRecordStatus.UnderReview,
            "operator",
            (int?)null,
            (DateTime?)null,
            (string?)null,
            It.Is<string?>(rn => rn != null && rn.Contains("SourceEntityName")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ReplayAsync_EmptyBody_PushesToUnderReview()
    {
        var record = BuildRecord(id: 5, body: "");
        _repoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var outcome = await target.ReplayAsync(5, "operator");

        Assert.AreEqual(DeadLetterReplayResult.InvalidRecord, outcome.Result);
    }

    [TestMethod]
    public async Task ReplayAsync_SenderFactoryThrows_PushesToUnderReview()
    {
        var record = BuildRecord(id: 5);
        _repoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _senderFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Throws(new InvalidOperationException("no sender"));

        var outcome = await target.ReplayAsync(5, "operator");

        Assert.AreEqual(DeadLetterReplayResult.InvalidRecord, outcome.Result);
        _repoMock.Verify(r => r.UpdateLifecycleAsync(
            5,
            DeadLetterRecordStatus.UnderReview,
            "operator",
            (int?)null,
            (DateTime?)null,
            (string?)null,
            It.Is<string?>(rn => rn != null && rn.Contains("no sender registered")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Send happy path ----

    [TestMethod]
    public async Task ReplayAsync_SendSucceeds_AdvancesToReplayedAndIncrementsAttempt()
    {
        var record = BuildRecord(id: 7, replayAttempts: 2);
        _repoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var outcome = await target.ReplayAsync(7, "operator");

        Assert.AreEqual(DeadLetterReplayResult.Sent, outcome.Result);
        Assert.IsNotNull(outcome.ReplayedMessageId);
        _repoMock.Verify(r => r.UpdateLifecycleAsync(
            7, DeadLetterRecordStatus.Replayed, "operator",
            3, // ReplayAttemptCount = 2 + 1
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ReplayAsync_SendSucceeds_MessagePreservesCorrelationIdAndContentType()
    {
        ServiceBusMessage? captured = null;
        var record = BuildRecord(id: 7, correlationId: "specific-corr", contentType: "text/plain");
        _repoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await target.ReplayAsync(7, "operator");

        Assert.IsNotNull(captured);
        Assert.AreEqual("specific-corr", captured!.CorrelationId);
        Assert.AreEqual("text/plain", captured.ContentType);
    }

    [TestMethod]
    public async Task ReplayAsync_SendSucceeds_MessageGetsFreshMessageId()
    {
        ServiceBusMessage? captured = null;
        var record = BuildRecord(id: 7);
        _repoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await target.ReplayAsync(7, "operator");

        Assert.IsNotNull(captured);
        Assert.AreNotEqual(record.MessageId, captured!.MessageId,
            "Replay must produce a fresh MessageId so it is distinguishable from the original");
        Assert.IsTrue(Guid.TryParseExact(captured.MessageId, "N", out _),
            $"Replay MessageId should be a 32-char no-dash GUID; got '{captured.MessageId}'");
    }

    [TestMethod]
    public async Task ReplayAsync_SendSucceeds_MessageStampsReplayedFromRecordIdAndEnqueuedTime()
    {
        ServiceBusMessage? captured = null;
        var record = BuildRecord(id: 99);
        _repoMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceBusMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await target.ReplayAsync(99, "operator");

        Assert.IsNotNull(captured);
        Assert.IsTrue(captured!.ApplicationProperties.ContainsKey("X-FileIt-ReplayedFromRecordId"));
        Assert.AreEqual("99", captured.ApplicationProperties["X-FileIt-ReplayedFromRecordId"]);
        Assert.IsTrue(captured.ApplicationProperties.ContainsKey(FileIt.Infrastructure.FileItMessageProperties.EnqueuedTimeUtc));
    }

    // ---- Send failure ----

    [TestMethod]
    public async Task ReplayAsync_SendFails_LeavesPendingAndReturnsSendFailed()
    {
        var record = BuildRecord(id: 7, replayAttempts: 1);
        _repoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("broker unavailable", ServiceBusFailureReason.ServiceCommunicationProblem));

        var outcome = await target.ReplayAsync(7, "operator");

        Assert.AreEqual(DeadLetterReplayResult.SendFailed, outcome.Result);
        Assert.IsNull(outcome.ReplayedMessageId);
        _repoMock.Verify(r => r.UpdateLifecycleAsync(
            7, DeadLetterRecordStatus.PendingReplay, "operator",
            2, // 1 + 1
            It.IsAny<DateTime?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ReplayAsync_SendThrowsOperationCanceled_RethrowsAndDoesNotUpdateLifecycle()
    {
        var record = BuildRecord(id: 7);
        _repoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.ReplayAsync(7, "operator"));

        _repoMock.Verify(r => r.UpdateLifecycleAsync(
            It.IsAny<long>(), It.IsAny<DeadLetterRecordStatus>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---- ReplayBatchAsync ----

    [TestMethod]
    public async Task ReplayBatchAsync_ZeroBatchSize_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => target.ReplayBatchAsync(0, "operator"));
    }

    [TestMethod]
    public async Task ReplayBatchAsync_NegativeBatchSize_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => target.ReplayBatchAsync(-5, "operator"));
    }

    [TestMethod]
    public async Task ReplayBatchAsync_NoRecords_ReturnsEmpty()
    {
        _repoMock
            .Setup(r => r.GetPendingReplayBatchAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DeadLetterRecord>());

        var outcomes = await target.ReplayBatchAsync(25, "timer");

        Assert.AreEqual(0, outcomes.Count);
    }

    [TestMethod]
    public async Task ReplayBatchAsync_HappyPath_ReplaysAllRecordsInOrder()
    {
        var records = new List<DeadLetterRecord>
        {
            BuildRecord(id: 1),
            BuildRecord(id: 2),
            BuildRecord(id: 3),
        };
        _repoMock
            .Setup(r => r.GetPendingReplayBatchAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => records.First(r => r.DeadLetterRecordId == id));
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var outcomes = await target.ReplayBatchAsync(25, "timer");

        Assert.AreEqual(3, outcomes.Count);
        Assert.IsTrue(outcomes.All(o => o.Result == DeadLetterReplayResult.Sent));
    }

    [TestMethod]
    public async Task ReplayBatchAsync_OneRecordFails_OthersStillProcess()
    {
        var records = new List<DeadLetterRecord>
        {
            BuildRecord(id: 1),
            BuildRecord(id: 2),
            BuildRecord(id: 3),
        };
        _repoMock
            .Setup(r => r.GetPendingReplayBatchAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => records.First(r => r.DeadLetterRecordId == id));

        // Only the second record's send fails.
        var sendCallCount = 0;
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns<ServiceBusMessage, CancellationToken>((msg, ct) =>
            {
                sendCallCount++;
                if (sendCallCount == 2)
                    return Task.FromException(new ServiceBusException("transient", ServiceBusFailureReason.ServiceTimeout));
                return Task.CompletedTask;
            });

        var outcomes = await target.ReplayBatchAsync(25, "timer");

        Assert.AreEqual(3, outcomes.Count);
        Assert.AreEqual(DeadLetterReplayResult.Sent, outcomes[0].Result);
        Assert.AreEqual(DeadLetterReplayResult.SendFailed, outcomes[1].Result);
        Assert.AreEqual(DeadLetterReplayResult.Sent, outcomes[2].Result);
    }

    [TestMethod]
    public async Task ReplayBatchAsync_CancellationMidBatch_StopsAndRethrows()
    {
        var records = new List<DeadLetterRecord>
        {
            BuildRecord(id: 1),
            BuildRecord(id: 2),
            BuildRecord(id: 3),
        };
        _repoMock
            .Setup(r => r.GetPendingReplayBatchAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);
        _repoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) => records.First(r => r.DeadLetterRecordId == id));

        var cts = new CancellationTokenSource();
        var sendCallCount = 0;
        _senderMock
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns<ServiceBusMessage, CancellationToken>((msg, ct) =>
            {
                sendCallCount++;
                if (sendCallCount == 1) cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.ReplayBatchAsync(25, "timer", cts.Token));
    }
}
