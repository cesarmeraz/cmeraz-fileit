using FileIt.Domain.Entities.DeadLetter;
using FileIt.Infrastructure.Data;

namespace FileIt.Infrastructure.Test.Data;

[TestClass]
public class TestDeadLetterRecordRepo
{
    public required InMemoryCommonDbContextFactory _factory;
    public required DeadLetterRecordRepo target;

    [TestInitialize]
    public void Setup()
    {
        _factory = new InMemoryCommonDbContextFactory();
        target = new DeadLetterRecordRepo(_factory);
    }

    [TestCleanup]
    public void Cleanup() => _factory.Dispose();

    private static DeadLetterRecord BuildRecord(
        string messageId = "msg-1",
        string sourceEntity = "dataflow-transform",
        DateTime? deadletteredAt = null,
        DeadLetterRecordStatus status = DeadLetterRecordStatus.New,
        FailureCategory category = FailureCategory.Poison)
    {
        return new DeadLetterRecord
        {
            MessageId = messageId,
            CorrelationId = "corr-1",
            SourceEntityType = SourceEntityType.Queue,
            SourceEntityName = sourceEntity,
            DeadLetterReason = "MaxDeliveryCountExceeded",
            DeadLetterErrorDescription = "test failure",
            DeliveryCount = 5,
            EnqueuedTimeUtc = DateTime.UtcNow.AddMinutes(-10),
            DeadLetteredTimeUtc = deadletteredAt ?? DateTime.UtcNow,
            FailureCategory = category,
            Status = status,
            MessageBody = "{}",
            ContentType = "application/json",
        };
    }

    // ---- Constructor null guard ----

    [TestMethod]
    public void Constructor_NullFactory_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DeadLetterRecordRepo(null!));
    }

    // ---- InsertAsync ----

    [TestMethod]
    public async Task InsertAsync_HappyPath_PersistsAndReturnsRecord()
    {
        var record = BuildRecord();

        var result = await target.InsertAsync(record);

        Assert.AreNotEqual(0L, result.DeadLetterRecordId);
        var fetched = await target.GetByIdAsync(result.DeadLetterRecordId);
        Assert.IsNotNull(fetched);
        Assert.AreEqual(record.MessageId, fetched!.MessageId);
    }

    [TestMethod]
    public async Task InsertAsync_NullRecord_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => target.InsertAsync(null!));
    }

    [TestMethod]
    public async Task InsertAsync_StampsCreatedUtcAndStatusUpdatedUtcWhenDefault()
    {
        var record = BuildRecord();
        record.CreatedUtc = default;
        record.StatusUpdatedUtc = default;
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await target.InsertAsync(record);

        Assert.IsTrue(result.CreatedUtc >= before);
        Assert.IsTrue(result.StatusUpdatedUtc >= before);
    }

    [TestMethod]
    public async Task InsertAsync_PreservesExistingCreatedUtcAndStatusUpdatedUtc()
    {
        var fixedTime = DateTime.UtcNow.AddDays(-2);
        var record = BuildRecord();
        record.CreatedUtc = fixedTime;
        record.StatusUpdatedUtc = fixedTime;

        var result = await target.InsertAsync(record);

        Assert.AreEqual(fixedTime, result.CreatedUtc);
        Assert.AreEqual(fixedTime, result.StatusUpdatedUtc);
    }

    // ---- ExistsAsync ----

    [TestMethod]
    public async Task ExistsAsync_ExistingTuple_ReturnsTrue()
    {
        var dlt = DateTime.UtcNow;
        var record = BuildRecord(messageId: "specific", sourceEntity: "queue-a", deadletteredAt: dlt);
        await target.InsertAsync(record);

        var exists = await target.ExistsAsync("specific", "queue-a", dlt);

        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_DifferentTuple_ReturnsFalse()
    {
        var dlt = DateTime.UtcNow;
        await target.InsertAsync(BuildRecord(messageId: "msg-A", sourceEntity: "queue-a", deadletteredAt: dlt));

        var exists = await target.ExistsAsync("msg-B", "queue-a", dlt);

        Assert.IsFalse(exists);
    }

    [TestMethod]
    public async Task ExistsAsync_EmptyMessageId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.ExistsAsync("", "q", DateTime.UtcNow));
    }

    [TestMethod]
    public async Task ExistsAsync_EmptySourceEntity_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.ExistsAsync("msg", "", DateTime.UtcNow));
    }

    // ---- GetByIdentityAsync ----

    [TestMethod]
    public async Task GetByIdentityAsync_Existing_ReturnsRecord()
    {
        var dlt = DateTime.UtcNow;
        var record = BuildRecord(messageId: "msg-X", sourceEntity: "q-X", deadletteredAt: dlt);
        await target.InsertAsync(record);

        var found = await target.GetByIdentityAsync("msg-X", "q-X", dlt);

        Assert.IsNotNull(found);
        Assert.AreEqual("msg-X", found!.MessageId);
    }

    [TestMethod]
    public async Task GetByIdentityAsync_Missing_ReturnsNull()
    {
        var found = await target.GetByIdentityAsync("missing", "q", DateTime.UtcNow);

        Assert.IsNull(found);
    }

    // ---- GetByIdAsync ----

    [TestMethod]
    public async Task GetByIdAsync_Existing_ReturnsRecord()
    {
        var inserted = await target.InsertAsync(BuildRecord());

        var found = await target.GetByIdAsync(inserted.DeadLetterRecordId);

        Assert.IsNotNull(found);
        Assert.AreEqual(inserted.DeadLetterRecordId, found!.DeadLetterRecordId);
    }

    [TestMethod]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        var found = await target.GetByIdAsync(999999);

        Assert.IsNull(found);
    }

    // ---- GetPendingReplayBatchAsync ----

    [TestMethod]
    public async Task GetPendingReplayBatchAsync_OnlyReturnsPendingReplay()
    {
        await target.InsertAsync(BuildRecord(messageId: "p1", status: DeadLetterRecordStatus.PendingReplay));
        await target.InsertAsync(BuildRecord(messageId: "n1", status: DeadLetterRecordStatus.New));
        await target.InsertAsync(BuildRecord(messageId: "r1", status: DeadLetterRecordStatus.Replayed));

        var batch = await target.GetPendingReplayBatchAsync(10);

        Assert.AreEqual(1, batch.Count);
        Assert.AreEqual("p1", batch[0].MessageId);
    }

    [TestMethod]
    public async Task GetPendingReplayBatchAsync_OrdersByStatusUpdatedAscending()
    {
        var older = BuildRecord(messageId: "older", status: DeadLetterRecordStatus.PendingReplay);
        older.StatusUpdatedUtc = DateTime.UtcNow.AddHours(-1);
        var newer = BuildRecord(messageId: "newer", status: DeadLetterRecordStatus.PendingReplay);
        newer.StatusUpdatedUtc = DateTime.UtcNow;
        await target.InsertAsync(older);
        await target.InsertAsync(newer);

        var batch = await target.GetPendingReplayBatchAsync(10);

        Assert.AreEqual(2, batch.Count);
        Assert.AreEqual("older", batch[0].MessageId);
        Assert.AreEqual("newer", batch[1].MessageId);
    }

    [TestMethod]
    public async Task GetPendingReplayBatchAsync_RespectsMaxRecords()
    {
        for (int i = 0; i < 5; i++)
        {
            await target.InsertAsync(BuildRecord(messageId: $"p{i}", status: DeadLetterRecordStatus.PendingReplay));
        }

        var batch = await target.GetPendingReplayBatchAsync(3);

        Assert.AreEqual(3, batch.Count);
    }

    [TestMethod]
    public async Task GetPendingReplayBatchAsync_ZeroMaxRecords_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => target.GetPendingReplayBatchAsync(0));
    }

    [TestMethod]
    public async Task GetPendingReplayBatchAsync_NegativeMaxRecords_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => target.GetPendingReplayBatchAsync(-3));
    }

    // ---- UpdateLifecycleAsync ----

    [TestMethod]
    public async Task UpdateLifecycleAsync_HappyPath_UpdatesAllProvidedFields()
    {
        var inserted = await target.InsertAsync(BuildRecord());

        await target.UpdateLifecycleAsync(
            deadLetterRecordId: inserted.DeadLetterRecordId,
            status: DeadLetterRecordStatus.Replayed,
            statusUpdatedBy: "operator",
            replayAttemptCount: 3,
            lastReplayAttemptUtc: DateTime.UtcNow,
            lastReplayMessageId: "new-msg-id",
            resolutionNotes: "Replayed successfully.");

        var fetched = await target.GetByIdAsync(inserted.DeadLetterRecordId);
        Assert.IsNotNull(fetched);
        Assert.AreEqual(DeadLetterRecordStatus.Replayed, fetched!.Status);
        Assert.AreEqual("operator", fetched.StatusUpdatedBy);
        Assert.AreEqual(3, fetched.ReplayAttemptCount);
        Assert.AreEqual("new-msg-id", fetched.LastReplayMessageId);
        Assert.AreEqual("Replayed successfully.", fetched.ResolutionNotes);
    }

    [TestMethod]
    public async Task UpdateLifecycleAsync_OnlyStatusProvided_LeavesOtherFieldsUntouched()
    {
        var record = BuildRecord();
        record.ReplayAttemptCount = 7;
        record.ResolutionNotes = "original notes";
        var inserted = await target.InsertAsync(record);

        await target.UpdateLifecycleAsync(
            inserted.DeadLetterRecordId,
            DeadLetterRecordStatus.UnderReview,
            statusUpdatedBy: "operator");

        var fetched = await target.GetByIdAsync(inserted.DeadLetterRecordId);
        Assert.IsNotNull(fetched);
        Assert.AreEqual(DeadLetterRecordStatus.UnderReview, fetched!.Status);
        Assert.AreEqual(7, fetched.ReplayAttemptCount); // preserved
        Assert.AreEqual("original notes", fetched.ResolutionNotes); // preserved
    }

    [TestMethod]
    public async Task UpdateLifecycleAsync_StampsStatusUpdatedUtc()
    {
        var inserted = await target.InsertAsync(BuildRecord());
        var before = DateTime.UtcNow.AddSeconds(-1);
        await Task.Delay(20);

        await target.UpdateLifecycleAsync(
            inserted.DeadLetterRecordId,
            DeadLetterRecordStatus.UnderReview,
            statusUpdatedBy: "operator");

        var fetched = await target.GetByIdAsync(inserted.DeadLetterRecordId);
        Assert.IsTrue(fetched!.StatusUpdatedUtc >= before);
    }

    [TestMethod]
    public async Task UpdateLifecycleAsync_MissingId_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => target.UpdateLifecycleAsync(
                deadLetterRecordId: 999999,
                status: DeadLetterRecordStatus.UnderReview,
                statusUpdatedBy: "operator"));
    }
}
