using System.Text;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.DeadLetter;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.DeadLetter.Replay;

/// <summary>
/// Default <see cref="IDeadLetterReplayService"/>. Composes
/// <see cref="IDeadLetterRecordRepo"/> and the named-sender factory into the
/// replay pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline for a single replay runs in a fixed order, with explicit
/// observability and lifecycle updates at every transition:
/// </para>
/// <list type="number">
///   <item><description>Load the record by id. Missing record returns <see cref="DeadLetterReplayResult.NotFound"/>; no lifecycle update.</description></item>
///   <item><description>Verify the record is in <see cref="DeadLetterRecordStatus.PendingReplay"/>. Wrong status returns <see cref="DeadLetterReplayResult.NotEligible"/>; no lifecycle update. Protects against double-replay races.</description></item>
///   <item><description>Validate replayability (source entity name, message body present). Invalid records are pushed to <see cref="DeadLetterRecordStatus.UnderReview"/> for operator attention with <see cref="DeadLetterReplayResult.InvalidRecord"/>.</description></item>
///   <item><description>Reconstruct the <see cref="ServiceBusMessage"/> from the persisted body, content type, and correlation id. Stamp publish-time metadata via <see cref="FileItMessageProperties"/> so the replayed message participates in the same observability discipline as a fresh publish.</description></item>
///   <item><description>Resolve the named sender for <see cref="DeadLetterRecord.SourceEntityName"/>. For topic subscriptions, the sender targets the topic, not the subscription; Service Bus filters the replay back to the original subscription via the same routing rules that produced the original delivery.</description></item>
///   <item><description>Send. On success, advance the record to <see cref="DeadLetterRecordStatus.Replayed"/>, increment <c>ReplayAttemptCount</c>, stamp <c>LastReplayMessageId</c>, return <see cref="DeadLetterReplayResult.Sent"/>. On broker failure, leave the record in <c>PendingReplay</c>, increment <c>ReplayAttemptCount</c>, return <see cref="DeadLetterReplayResult.SendFailed"/>; subsequent runs retry.</description></item>
/// </list>
/// <para>
/// The batch path delegates to the single-record path inside a per-record try/catch
/// so a transient failure on record N does not abort the rest of the batch.
/// Programmer errors (null factory, etc.) still propagate; only operational failures
/// are isolated.
/// </para>
/// </remarks>
public sealed class DeadLetterReplayService : IDeadLetterReplayService
{
    private readonly IDeadLetterRecordRepo _repo;
    private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
    private readonly ILogger<DeadLetterReplayService> _logger;

    public DeadLetterReplayService(
        IDeadLetterRecordRepo repo,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        ILogger<DeadLetterReplayService> logger)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _senderFactory = senderFactory ?? throw new ArgumentNullException(nameof(senderFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeadLetterReplayOutcome> ReplayAsync(
        long deadLetterRecordId,
        string initiatedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(initiatedBy);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            InfrastructureEvents.ReplayInitiated,
            "Replay initiated for DeadLetterRecord {DeadLetterRecordId} by {InitiatedBy}.",
            deadLetterRecordId,
            initiatedBy);

        var record = await _repo.GetByIdAsync(deadLetterRecordId, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            _logger.LogWarning(
                InfrastructureEvents.ReplayFailed,
                "Replay aborted: DeadLetterRecord {DeadLetterRecordId} not found.",
                deadLetterRecordId);

            return new DeadLetterReplayOutcome(
                DeadLetterRecordId: deadLetterRecordId,
                Result: DeadLetterReplayResult.NotFound,
                ReplayedMessageId: null,
                Reason: "Record not found.");
        }

        if (record.Status != DeadLetterRecordStatus.PendingReplay)
        {
            _logger.LogInformation(
                InfrastructureEvents.ReplayFailed,
                "Replay skipped for DeadLetterRecord {DeadLetterRecordId}: status is "
                    + "{Status}, expected PendingReplay.",
                deadLetterRecordId,
                record.Status);

            return new DeadLetterReplayOutcome(
                DeadLetterRecordId: deadLetterRecordId,
                Result: DeadLetterReplayResult.NotEligible,
                ReplayedMessageId: null,
                Reason: $"Status is {record.Status}, expected PendingReplay.");
        }

        if (!IsStructurallyReplayable(record, out var validationReason))
        {
            await _repo.UpdateLifecycleAsync(
                    deadLetterRecordId: record.DeadLetterRecordId,
                    status: DeadLetterRecordStatus.UnderReview,
                    statusUpdatedBy: initiatedBy,
                    resolutionNotes: $"Replay aborted: {validationReason}",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogError(
                InfrastructureEvents.ReplayFailed,
                "Replay aborted for DeadLetterRecord {DeadLetterRecordId}: {Reason}. "
                    + "Record pushed to UnderReview.",
                deadLetterRecordId,
                validationReason);

            return new DeadLetterReplayOutcome(
                DeadLetterRecordId: deadLetterRecordId,
                Result: DeadLetterReplayResult.InvalidRecord,
                ReplayedMessageId: null,
                Reason: validationReason);
        }

        var replayMessage = BuildReplayMessage(record);

        ServiceBusSender sender;
        try
        {
            sender = _senderFactory.CreateClient(record.SourceEntityName);
        }
        catch (Exception ex)
        {
            await _repo.UpdateLifecycleAsync(
                    deadLetterRecordId: record.DeadLetterRecordId,
                    status: DeadLetterRecordStatus.UnderReview,
                    statusUpdatedBy: initiatedBy,
                    resolutionNotes:
                        $"Replay aborted: no sender registered for "
                        + $"'{record.SourceEntityName}'. Configure AddAzureClients in "
                        + $"AddInfrastructure to include this entity.",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogError(
                InfrastructureEvents.ReplayFailed,
                ex,
                "Replay aborted for DeadLetterRecord {DeadLetterRecordId}: no sender "
                    + "registered for source '{SourceEntityName}'.",
                deadLetterRecordId,
                record.SourceEntityName);

            return new DeadLetterReplayOutcome(
                DeadLetterRecordId: deadLetterRecordId,
                Result: DeadLetterReplayResult.InvalidRecord,
                ReplayedMessageId: null,
                Reason: $"No sender registered for '{record.SourceEntityName}'.");
        }

        try
        {
            await sender.SendMessageAsync(replayMessage, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation. Leave the record in PendingReplay and let
            // a subsequent run retry; do not falsely advance the lifecycle.
            throw;
        }
        catch (Exception ex)
        {
            await _repo.UpdateLifecycleAsync(
                    deadLetterRecordId: record.DeadLetterRecordId,
                    status: DeadLetterRecordStatus.PendingReplay,
                    statusUpdatedBy: initiatedBy,
                    replayAttemptCount: record.ReplayAttemptCount + 1,
                    lastReplayAttemptUtc: DateTime.UtcNow,
                    resolutionNotes:
                        $"Replay attempt {record.ReplayAttemptCount + 1} failed: {ex.Message}",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _logger.LogError(
                InfrastructureEvents.ReplayFailed,
                ex,
                "Replay send failed for DeadLetterRecord {DeadLetterRecordId} to "
                    + "'{SourceEntityName}' (attempt {ReplayAttemptCount}).",
                deadLetterRecordId,
                record.SourceEntityName,
                record.ReplayAttemptCount + 1);

            return new DeadLetterReplayOutcome(
                DeadLetterRecordId: deadLetterRecordId,
                Result: DeadLetterReplayResult.SendFailed,
                ReplayedMessageId: null,
                Reason: ex.Message);
        }

        await _repo.UpdateLifecycleAsync(
                deadLetterRecordId: record.DeadLetterRecordId,
                status: DeadLetterRecordStatus.Replayed,
                statusUpdatedBy: initiatedBy,
                replayAttemptCount: record.ReplayAttemptCount + 1,
                lastReplayAttemptUtc: DateTime.UtcNow,
                lastReplayMessageId: replayMessage.MessageId,
                resolutionNotes:
                    $"Replayed by {initiatedBy} as MessageId {replayMessage.MessageId}.",
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            InfrastructureEvents.ReplaySucceeded,
            "Replayed DeadLetterRecord {DeadLetterRecordId} to '{SourceEntityName}' as "
                + "MessageId {ReplayedMessageId} (attempt {ReplayAttemptCount}).",
            deadLetterRecordId,
            record.SourceEntityName,
            replayMessage.MessageId,
            record.ReplayAttemptCount + 1);

        return new DeadLetterReplayOutcome(
            DeadLetterRecordId: deadLetterRecordId,
            Result: DeadLetterReplayResult.Sent,
            ReplayedMessageId: replayMessage.MessageId,
            Reason: $"Sent to {record.SourceEntityName}.");
    }

    public async Task<IReadOnlyList<DeadLetterReplayOutcome>> ReplayBatchAsync(
        int maxRecords,
        string initiatedBy,
        CancellationToken cancellationToken = default)
    {
        if (maxRecords <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRecords),
                maxRecords,
                "Batch size must be positive.");
        }
        ArgumentException.ThrowIfNullOrEmpty(initiatedBy);
        cancellationToken.ThrowIfCancellationRequested();

        var batch = await _repo.GetPendingReplayBatchAsync(maxRecords, cancellationToken)
            .ConfigureAwait(false);

        if (batch.Count == 0)
        {
            _logger.LogInformation(
                InfrastructureEvents.ReplayFunctionStarted,
                "Replay batch initiated by {InitiatedBy}: no PendingReplay records.",
                initiatedBy);
            return Array.Empty<DeadLetterReplayOutcome>();
        }

        _logger.LogInformation(
            InfrastructureEvents.ReplayFunctionStarted,
            "Replay batch initiated by {InitiatedBy}: processing {RecordCount} records.",
            initiatedBy,
            batch.Count);

        var outcomes = new List<DeadLetterReplayOutcome>(batch.Count);
        foreach (var record in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var outcome = await ReplayAsync(
                        record.DeadLetterRecordId,
                        initiatedBy,
                        cancellationToken)
                    .ConfigureAwait(false);
                outcomes.Add(outcome);
            }
            catch (OperationCanceledException)
            {
                // Bail out of the loop without swallowing. Records not yet processed
                // remain at PendingReplay for the next batch run.
                throw;
            }
            catch (Exception ex)
            {
                // Per-record isolation: any unexpected failure is recorded as a
                // SendFailed outcome so the batch continues. The single-record path
                // already handles known operational failures explicitly; reaching
                // this catch indicates a defect or an infrastructure failure not
                // anticipated by ReplayAsync.
                _logger.LogError(
                    InfrastructureEvents.ReplayFailed,
                    ex,
                    "Unexpected error replaying DeadLetterRecord {DeadLetterRecordId} "
                        + "in batch initiated by {InitiatedBy}.",
                    record.DeadLetterRecordId,
                    initiatedBy);

                outcomes.Add(new DeadLetterReplayOutcome(
                    DeadLetterRecordId: record.DeadLetterRecordId,
                    Result: DeadLetterReplayResult.SendFailed,
                    ReplayedMessageId: null,
                    Reason: $"Unexpected error: {ex.Message}"));
            }
        }

        _logger.LogInformation(
            InfrastructureEvents.ReplayFunctionStopped,
            "Replay batch complete: Sent={SentCount}, NotEligible={NotEligibleCount}, "
                + "InvalidRecord={InvalidCount}, SendFailed={SendFailedCount}, "
                + "NotFound={NotFoundCount}.",
            outcomes.Count(o => o.Result == DeadLetterReplayResult.Sent),
            outcomes.Count(o => o.Result == DeadLetterReplayResult.NotEligible),
            outcomes.Count(o => o.Result == DeadLetterReplayResult.InvalidRecord),
            outcomes.Count(o => o.Result == DeadLetterReplayResult.SendFailed),
            outcomes.Count(o => o.Result == DeadLetterReplayResult.NotFound));

        return outcomes;
    }

    /// <summary>
    /// Validates the structural invariants needed to replay. The classifier and the
    /// reader should have produced a record that satisfies all of these; reaching a
    /// failure here means data has been hand-edited or an upstream defect exists.
    /// </summary>
    private static bool IsStructurallyReplayable(
        DeadLetterRecord record,
        out string reason)
    {
        if (string.IsNullOrWhiteSpace(record.SourceEntityName))
        {
            reason = "SourceEntityName is empty.";
            return false;
        }
        if (string.IsNullOrEmpty(record.MessageBody))
        {
            reason = "MessageBody is empty; replay requires the original payload.";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Reconstructs a <see cref="ServiceBusMessage"/> from a persisted record. The
    /// new message gets a fresh MessageId so it is distinguishable in CommonLog from
    /// the original; CorrelationId, ContentType, and the publish-time stamp are
    /// preserved (or freshly applied) so downstream consumers see a normal message
    /// indistinguishable from a fresh publish.
    /// </summary>
    private static ServiceBusMessage BuildReplayMessage(DeadLetterRecord record)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(record.MessageBody);
        var message = new ServiceBusMessage(BinaryData.FromBytes(bodyBytes))
        {
            MessageId = Guid.NewGuid().ToString("N"),
            CorrelationId = record.CorrelationId,
            ContentType = record.ContentType,
        };

        // Stamp publish-time UTC just like BusTool/PublishTool do for fresh messages.
        // This keeps the failure-age delta computation honest if this replay also
        // dead-letters: the stamped time reflects when the replay happened, which is
        // the right "original publish" timestamp for the second go-round.
        message.ApplicationProperties[FileItMessageProperties.EnqueuedTimeUtc] =
            DateTime.UtcNow.ToString("O");

        // Mark replays explicitly so downstream consumers and observability tooling
        // can distinguish them from organic publishes if they care to. Most
        // consumers ignore this property; the FileIt UI uses it to badge replayed
        // messages in the timeline view.
        message.ApplicationProperties["X-FileIt-ReplayedFromRecordId"] =
            record.DeadLetterRecordId.ToString();

        return message;
    }
}