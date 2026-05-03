using FileIt.Domain.Entities.DeadLetter;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Classification;
using FileIt.Infrastructure.DeadLetter.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.DeadLetter.Ingestion;

[TestClass]
public class TestDeadLetterIngestionService
{
    public required Mock<IDeadLetterClassifier> _classifierMock;
    public required Mock<IDeadLetterRecordRepo> _repoMock;
    public required Mock<ILogger<DeadLetterIngestionService>> _loggerMock;
    public required DeadLetterIngestionService target;

    [TestInitialize]
    public void Setup()
    {
        _classifierMock = new Mock<IDeadLetterClassifier>();
        _repoMock = new Mock<IDeadLetterRecordRepo>();
        _loggerMock = new Mock<ILogger<DeadLetterIngestionService>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );

        target = new DeadLetterIngestionService(
            _classifierMock.Object,
            _repoMock.Object,
            _loggerMock.Object);
    }

    private static DeadLetterIngestionEnvelope BuildEnvelope(
        string messageId = "msg-1",
        string sourceEntity = "dataflow-transform",
        SourceEntityType sourceType = SourceEntityType.Queue,
        string? subscription = null,
        string? reason = "MaxDeliveryCountExceeded",
        string? description = "test failure",
        int deliveryCount = 5,
        DateTime? enqueued = null,
        DateTime? deadlettered = null,
        string body = "{}",
        IReadOnlyDictionary<string, object?>? appProps = null)
    {
        return DeadLetterIngestionEnvelope.Create(
            messageId: messageId,
            correlationId: "corr-1",
            sessionId: null,
            sourceEntityType: sourceType,
            sourceEntityName: sourceEntity,
            sourceSubscriptionName: subscription,
            deadLetterReason: reason,
            deadLetterErrorDescription: description,
            deliveryCount: deliveryCount,
            enqueuedTimeUtc: enqueued ?? DateTime.UtcNow.AddMinutes(-5),
            deadLetteredTimeUtc: deadlettered ?? DateTime.UtcNow,
            messageBody: body,
            messageProperties: null,
            contentType: "application/json",
            applicationProperties: appProps ?? new Dictionary<string, object?>());
    }

    private static DeadLetterClassification BuildClassification(
        FailureCategory category = FailureCategory.Poison,
        string rule = "BuiltInReason_MaxDeliveryCountExceeded")
    {
        return new DeadLetterClassification(
            Category: category,
            Reasoning: $"test reasoning for {category}",
            MatchedRule: rule);
    }

    // ---- Constructor null guards ----

    [TestMethod]
    public void Constructor_NullClassifier_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeadLetterIngestionService(null!, _repoMock.Object, _loggerMock.Object));
    }

    [TestMethod]
    public void Constructor_NullRepo_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeadLetterIngestionService(_classifierMock.Object, null!, _loggerMock.Object));
    }

    [TestMethod]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DeadLetterIngestionService(_classifierMock.Object, _repoMock.Object, null!));
    }

    // ---- IngestAsync: input guards ----

    [TestMethod]
    public async Task IngestAsync_NullEnvelope_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => target.IngestAsync(null!));
    }

    [TestMethod]
    public async Task IngestAsync_CancellationRequested_ThrowsBeforeWork()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var envelope = BuildEnvelope();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.IngestAsync(envelope, cts.Token));

        _classifierMock.Verify(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()), Times.Never);
        _repoMock.Verify(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Happy path ----

    [TestMethod]
    public async Task IngestAsync_HappyPath_CallsClassifierThenInsert()
    {
        var envelope = BuildEnvelope();
        var classification = BuildClassification(FailureCategory.Poison);
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Returns(classification);
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetterRecord r, CancellationToken _) =>
            {
                r.DeadLetterRecordId = 42;
                return r;
            });

        var result = await target.IngestAsync(envelope);

        Assert.AreEqual(42, result.DeadLetterRecordId);
        _classifierMock.Verify(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()), Times.Once);
        _repoMock.Verify(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task IngestAsync_PassesEnvelopeFieldsToClassifierInput()
    {
        DeadLetterClassificationInput? capturedInput = null;
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Callback<DeadLetterClassificationInput>(input => capturedInput = input)
            .Returns(BuildClassification());
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetterRecord r, CancellationToken _) => r);

        var envelope = BuildEnvelope(
            reason: "TTLExpiredException",
            description: "aged out",
            deliveryCount: 3,
            sourceEntity: "my-q");

        await target.IngestAsync(envelope);

        Assert.IsNotNull(capturedInput);
        Assert.AreEqual("TTLExpiredException", capturedInput!.DeadLetterReason);
        Assert.AreEqual("aged out", capturedInput.DeadLetterErrorDescription);
        Assert.AreEqual(3, capturedInput.DeliveryCount);
        Assert.AreEqual("my-q", capturedInput.SourceEntityName);
    }

    [TestMethod]
    public async Task IngestAsync_BuildsRecordFromEnvelopeAndClassification()
    {
        DeadLetterRecord? captured = null;
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Returns(BuildClassification(FailureCategory.SchemaViolation, "Heuristic_Schema_JsonException"));
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .Callback<DeadLetterRecord, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync((DeadLetterRecord r, CancellationToken _) => { r.DeadLetterRecordId = 1; return r; });

        var envelope = BuildEnvelope(messageId: "specific-msg-id", sourceEntity: "specific-queue");

        await target.IngestAsync(envelope);

        Assert.IsNotNull(captured);
        Assert.AreEqual("specific-msg-id", captured!.MessageId);
        Assert.AreEqual("specific-queue", captured.SourceEntityName);
        Assert.AreEqual(FailureCategory.SchemaViolation, captured.FailureCategory);
        StringAssert.Contains(captured.ResolutionNotes ?? "", "test reasoning");
    }

    // ---- Idempotency conflict path ----

    [TestMethod]
    public async Task IngestAsync_IdempotencyConflict_ReturnsExistingRecord()
    {
        var envelope = BuildEnvelope();
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Returns(BuildClassification());

        // SqlException with Number=2601 (unique index violation)
        var sqlEx = MakeSqlException(2601);
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique violation", sqlEx));

        var existing = new DeadLetterRecord { DeadLetterRecordId = 99, MessageId = envelope.MessageId };
        _repoMock
            .Setup(r => r.GetByIdentityAsync(
                envelope.MessageId,
                envelope.SourceEntityName,
                envelope.DeadLetteredTimeUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await target.IngestAsync(envelope);

        Assert.AreEqual(99, result.DeadLetterRecordId);
        _repoMock.Verify(r => r.GetByIdentityAsync(
            envelope.MessageId, envelope.SourceEntityName, envelope.DeadLetteredTimeUtc, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task IngestAsync_IdempotencyConflictButNoExistingRow_RethrowsAsDefect()
    {
        var envelope = BuildEnvelope();
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Returns(BuildClassification());

        var sqlEx = MakeSqlException(2627); // unique constraint violation
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("unique violation", sqlEx));
        _repoMock
            .Setup(r => r.GetByIdentityAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeadLetterRecord?)null);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => target.IngestAsync(envelope));
    }

    // ---- Persistence failure path ----

    [TestMethod]
    public async Task IngestAsync_NonIdempotencyDbUpdateException_Rethrows()
    {
        var envelope = BuildEnvelope();
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Returns(BuildClassification());

        // SqlException with a non-idempotency Number (e.g. 547 = FK violation)
        var sqlEx = MakeSqlException(547);
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("FK violation", sqlEx));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => target.IngestAsync(envelope));

        // Should NOT call GetByIdentityAsync because this isn't idempotency conflict
        _repoMock.Verify(r => r.GetByIdentityAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task IngestAsync_GenericPersistenceException_Rethrows()
    {
        var envelope = BuildEnvelope();
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Returns(BuildClassification());
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("disk full"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => target.IngestAsync(envelope));
    }

    [TestMethod]
    public async Task IngestAsync_OperationCanceledFromRepo_Rethrows()
    {
        var envelope = BuildEnvelope();
        _classifierMock
            .Setup(c => c.Classify(It.IsAny<DeadLetterClassificationInput>()))
            .Returns(BuildClassification());
        _repoMock
            .Setup(r => r.InsertAsync(It.IsAny<DeadLetterRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.IngestAsync(envelope));
    }

    // ---- Helper ----

    private static Microsoft.Data.SqlClient.SqlException MakeSqlException(int number)
    {
        // Building Microsoft.Data.SqlClient.SqlException without a real connection
        // requires reflecting against several internal members. The constructor is
        // internal, the inner List<SqlError> isn't initialized via FormatterServices,
        // and the public SqlErrorCollection.Add path NREs without it. Approach:
        // 1) FormatterServices.GetUninitializedObject(SqlErrorCollection)
        // 2) Reflect into _errors and assign a fresh List<SqlError>
        // 3) Build a SqlError with Number set via _number reflection
        // 4) Add error to the list directly (skipping SqlErrorCollection.Add)
        // 5) FormatterServices.GetUninitializedObject(SqlException) and set _errors.
        var bf = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        var errorType = typeof(Microsoft.Data.SqlClient.SqlError);
        var error = (Microsoft.Data.SqlClient.SqlError)
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(errorType);
        errorType.GetField("_number", bf)?.SetValue(error, number);

        var collectionType = typeof(Microsoft.Data.SqlClient.SqlErrorCollection);
        var collection = (Microsoft.Data.SqlClient.SqlErrorCollection)
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(collectionType);
        // initialize the inner list (its actual generic type may be List<object> in some
        // SqlClient versions, so use the field's declared type to construct it)
        var listField = collectionType.GetField("_errors", bf);
        if (listField != null)
        {
            var listType = listField.FieldType;
            var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
            list.Add(error);
            listField.SetValue(collection, list);
        }

        var exType = typeof(Microsoft.Data.SqlClient.SqlException);
        var ex = (Microsoft.Data.SqlClient.SqlException)
            System.Runtime.Serialization.FormatterServices.GetUninitializedObject(exType);
        exType.GetField("_errors", bf)?.SetValue(ex, collection);

        return ex;
    }
}
