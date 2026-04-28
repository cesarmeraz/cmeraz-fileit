using FileIt.Domain.Entities.DeadLetter;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Classification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.DeadLetter.Ingestion;

/// <summary>
/// Default <see cref="IDeadLetterIngestionService"/>. Composes the classifier and
/// the repo into a single ingestion pipeline with explicit observability at every phase.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline runs in a fixed order:
/// <list type="number">
///   <item><description>Emit <c>DeadLetterMessageReceived</c> (52). Pre-work checkpoint so log timelines have a clear entry event even if subsequent steps fail.</description></item>
///   <item><description>Classify via <see cref="IDeadLetterClassifier"/>. Emit <c>DeadLetterClassified</c> (55) on success; <c>DeadLetterClassificationUnknown</c> (56) when the classifier returned <see cref="FailureCategory.Unknown"/>.</description></item>
///   <item><description>Construct the entity and call <see cref="IDeadLetterRecordRepo.InsertAsync"/>. Emit <c>DeadLetterRecordPersisted</c> (53) on success.</description></item>
///   <item><description>On <see cref="DbUpdateException"/> from the unique idempotency index, fetch the pre-existing row and emit <c>DeadLetterRecordPersisted</c> (53) with an <c>idempotent=true</c> property. Operationally indistinguishable from a fresh insert; the audit trail records the duplicate delivery without alerting on it.</description></item>
///   <item><description>On any other persistence failure, emit <c>DeadLetterRecordPersistFailed</c> (54) and rethrow. The reader's own retry/abandon machinery handles redelivery.</description></item>
/// </list>
/// </para>
/// <para>
/// Every log entry includes <c>SourceEntityName</c>, <c>MessageId</c>, and (when
/// non-null) <c>CorrelationId</c> as structured properties so the resulting CommonLog
/// rows are queryable by the same identifiers operators see in the FileIt UI.
/// </para>
/// </remarks>
public sealed class DeadLetterIngestionService : IDeadLetterIngestionService
{
    private readonly IDeadLetterClassifier _classifier;
    private readonly IDeadLetterRecordRepo _repo;
    private readonly ILogger<DeadLetterIngestionService> _logger;

    public DeadLetterIngestionService(
        IDeadLetterClassifier classifier,
        IDeadLetterRecordRepo repo,
        ILogger<DeadLetterIngestionService> logger)
    {
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeadLetterRecord> IngestAsync(
        DeadLetterIngestionEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            InfrastructureEvents.DeadLetterMessageReceived,
            "Ingesting dead-lettered message {MessageId} from {SourceEntityName} "
                + "(DeliveryCount={DeliveryCount}, Reason={DeadLetterReason})",
            envelope.MessageId,
            envelope.SourceEntityName,
            envelope.DeliveryCount,
            envelope.DeadLetterReason ?? "<null>");

        var classification = ClassifyOrThrow(envelope);

        var record = BuildRecord(envelope, classification);

        try
        {
            var persisted = await _repo.InsertAsync(record, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                InfrastructureEvents.DeadLetterRecordPersisted,
                "Persisted DeadLetterRecord {DeadLetterRecordId} for message "
                    + "{MessageId} from {SourceEntityName} as {FailureCategory} "
                    + "(idempotent={Idempotent})",
                persisted.DeadLetterRecordId,
                envelope.MessageId,
                envelope.SourceEntityName,
                classification.Category,
                false);

            return persisted;
        }
        catch (DbUpdateException ex) when (IsIdempotencyConflict(ex))
        {
            // The unique index IX_DeadLetterRecord_MessageId_Source_DeadLetteredTime
            // rejected this insert because a previous delivery already recorded the
            // same dead letter. This is success from the system's standpoint: exactly
            // one row exists for this dead-letter event. Fetch the canonical row so
            // the caller can act on it the same way as a fresh insert.
            var existing = await FindExistingAsync(envelope, cancellationToken)
                .ConfigureAwait(false);

            if (existing is null)
            {
                // The unique-index violation said the row exists, but our identity-tuple
                // lookup did not find it. Either the index covers a different tuple than
                // we think it does, or another path is racing. Surface as a defect.
                _logger.LogError(
                    InfrastructureEvents.DeadLetterRecordPersistFailed,
                    ex,
                    "Idempotency conflict on insert for message {MessageId} from "
                        + "{SourceEntityName}, but no matching row found on lookup.",
                    envelope.MessageId,
                    envelope.SourceEntityName);
                throw;
            }

            _logger.LogInformation(
                InfrastructureEvents.DeadLetterRecordPersisted,
                "Idempotent re-ingest of DeadLetterRecord {DeadLetterRecordId} for "
                    + "message {MessageId} from {SourceEntityName} (no new row inserted).",
                existing.DeadLetterRecordId,
                envelope.MessageId,
                envelope.SourceEntityName);

            return existing;
        }
        catch (OperationCanceledException)
        {
            // Cooperative cancellation. Not an ingestion failure; let the reader
            // unwind cleanly without a misleading PersistFailed event.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                InfrastructureEvents.DeadLetterRecordPersistFailed,
                ex,
                "Failed to persist DeadLetterRecord for message {MessageId} from "
                    + "{SourceEntityName}.",
                envelope.MessageId,
                envelope.SourceEntityName);
            throw;
        }
    }

    private DeadLetterClassification ClassifyOrThrow(DeadLetterIngestionEnvelope envelope)
    {
        var input = new DeadLetterClassificationInput(
            DeadLetterReason: envelope.DeadLetterReason,
            DeadLetterErrorDescription: envelope.DeadLetterErrorDescription,
            DeliveryCount: envelope.DeliveryCount,
            SourceEntityName: envelope.SourceEntityName,
            ApplicationProperties: envelope.ApplicationProperties);

        var classification = _classifier.Classify(input);

        if (classification.Category == FailureCategory.Unknown)
        {
            _logger.LogWarning(
                InfrastructureEvents.DeadLetterClassificationUnknown,
                "Classifier could not determine category for message {MessageId} "
                    + "from {SourceEntityName}: {Reasoning}",
                envelope.MessageId,
                envelope.SourceEntityName,
                classification.Reasoning);
        }
        else
        {
            _logger.LogInformation(
                InfrastructureEvents.DeadLetterClassified,
                "Classified message {MessageId} from {SourceEntityName} as "
                    + "{FailureCategory} via rule {MatchedRule}: {Reasoning}",
                envelope.MessageId,
                envelope.SourceEntityName,
                classification.Category,
                classification.MatchedRule,
                classification.Reasoning);
        }

        return classification;
    }

    private static DeadLetterRecord BuildRecord(
        DeadLetterIngestionEnvelope envelope,
        DeadLetterClassification classification)
    {
        return new DeadLetterRecord
        {
            MessageId = envelope.MessageId,
            CorrelationId = envelope.CorrelationId,
            SessionId = envelope.SessionId,

            SourceEntityType = envelope.SourceEntityType,
            SourceEntityName = envelope.SourceEntityName,
            SourceSubscriptionName = envelope.SourceSubscriptionName,

            DeadLetterReason = envelope.DeadLetterReason,
            DeadLetterErrorDescription = envelope.DeadLetterErrorDescription,
            DeliveryCount = envelope.DeliveryCount,
            EnqueuedTimeUtc = envelope.EnqueuedTimeUtc,
            DeadLetteredTimeUtc = envelope.DeadLetteredTimeUtc,

            FailureCategory = classification.Category,

            MessageBody = envelope.MessageBody,
            MessageProperties = envelope.MessageProperties,
            ContentType = envelope.ContentType,

            // Status, StatusUpdatedUtc, ReplayAttemptCount default at the database
            // (DF_DeadLetterRecord_*); see DeadLetterRecordRepo.InsertAsync for the
            // explicit application-side timestamp pin that prevents clock drift.

            ResolutionNotes = classification.Reasoning,
        };
    }

    /// <summary>
    /// Heuristic detector for the unique-index violation produced by
    /// <c>IX_DeadLetterRecord_MessageId_Source_DeadLetteredTime</c>. Inspects the
    /// inner SQL exception's number (2601 = unique index, 2627 = unique constraint).
    /// </summary>
    private static bool IsIdempotencyConflict(DbUpdateException ex)
    {
        for (Exception? cur = ex; cur is not null; cur = cur.InnerException)
        {
            if (cur is Microsoft.Data.SqlClient.SqlException sql)
            {
                if (sql.Number == 2601 || sql.Number == 2627)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private async Task<DeadLetterRecord?> FindExistingAsync(
        DeadLetterIngestionEnvelope envelope,
        CancellationToken cancellationToken)
    {
        // Single targeted lookup against the same unique tuple that just rejected
        // our INSERT. The repo's GetByIdentityAsync uses an index seek on
        // IX_DeadLetterRecord_MessageId_Source_DeadLetteredTime and is therefore
        // O(log n) regardless of table size. Returns null only if the unique-index
        // violation reflects a race that resolved before we re-read, which is a
        // defect signal handled by the caller's error path.
        return await _repo.GetByIdentityAsync(
                envelope.MessageId,
                envelope.SourceEntityName,
                envelope.DeadLetteredTimeUtc,
                cancellationToken)
            .ConfigureAwait(false);
    }

}
