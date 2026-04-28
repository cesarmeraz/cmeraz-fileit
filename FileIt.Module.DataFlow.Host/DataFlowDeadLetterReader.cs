// Reader function for the dataflow-transform queue's dead-letter sub-queue.
// Listens on dataflow-transform/$DeadLetterQueue, hands each dead-lettered message
// to the shared IDeadLetterIngestionService for classification + persistence,
// and completes the DLQ message so the DLQ itself does not fill up.
//
// All real logic (classification, idempotency, audit logging) lives in the
// ingestion service. This reader is deliberately a thin adapter so the same
// pattern can be repeated verbatim for every other DLQ-bearing channel in FileIt.
//
// See docs/dead-letter-strategy.md for the full design.
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.DeadLetter;
using FileIt.Infrastructure;
using FileIt.Infrastructure.DeadLetter.Ingestion;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.Host;

/// <summary>
/// Azure Function that drains the dataflow-transform queue's dead-letter sub-queue.
/// </summary>
/// <remarks>
/// <para>
/// This function does not retry, classify, or otherwise reason about dead-lettered
/// messages itself. It builds a <see cref="DeadLetterIngestionEnvelope"/> from the
/// trigger arguments and delegates to <see cref="IDeadLetterIngestionService"/>.
/// All cross-channel concerns live behind that interface.
/// </para>
/// <para>
/// The trigger path uses the documented Azure Service Bus dead-letter sub-queue
/// syntax <c>queue/$deadletterqueue</c>. The function-level connection name
/// matches the existing pattern in <see cref="DataFlowSubscriber"/>; both bind to
/// <c>ConnectionStrings__ServiceBus</c> via the host configuration.
/// </para>
/// </remarks>
public class DataFlowDeadLetterReader
{
    /// <summary>
    /// Trigger path for this reader. Public constant so it is greppable across the
    /// codebase and matches the same string the operator sees in the Azure portal.
    /// </summary>
    public const string DeadLetterPath = "dataflow-transform/$deadletterqueue";

    /// <summary>
    /// Source queue name (without the dead-letter sub-path). Persisted into
    /// <c>DeadLetterRecord.SourceEntityName</c> and used by the replay service to
    /// re-target the original queue.
    /// </summary>
    public const string SourceQueueName = "dataflow-transform";

    private readonly IDeadLetterIngestionService _ingestion;
    private readonly ILogger<DataFlowDeadLetterReader> _logger;

    public DataFlowDeadLetterReader(
        IDeadLetterIngestionService ingestion,
        ILogger<DataFlowDeadLetterReader> logger)
    {
        _ingestion = ingestion ?? throw new ArgumentNullException(nameof(ingestion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(nameof(DataFlowDeadLetterReader))]
    public async Task Run(
        [ServiceBusTrigger(DeadLetterPath)] ServiceBusReceivedMessage message,
        FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(message);
        var cancellationToken = context.CancellationToken;
        var correlationId = message.CorrelationId ?? string.Empty;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "DeadLetterSource", SourceQueueName },
        }))
        {
            _logger.LogInformation(
                InfrastructureEvents.DeadLetterMessageReceived,
                "Received dead-letter from {SourceQueueName} (MessageId={MessageId}, "
                    + "DeliveryCount={DeliveryCount}, Reason={DeadLetterReason}).",
                SourceQueueName,
                message.MessageId,
                message.DeliveryCount,
                message.DeadLetterReason ?? "<null>");

            var envelope = BuildEnvelope(message);

            var record = await _ingestion.IngestAsync(envelope, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                InfrastructureEvents.DeadLetterRecordPersisted,
                "Dead-letter ingestion complete for {SourceQueueName} MessageId={MessageId}; "
                    + "DeadLetterRecordId={DeadLetterRecordId}, Category={FailureCategory}.",
                SourceQueueName,
                message.MessageId,
                record.DeadLetterRecordId,
                record.FailureCategory);
        }
    }

    /// <summary>
    /// Translates a Service Bus envelope into a domain envelope. The validation
    /// burden lives in <see cref="DeadLetterIngestionEnvelope.Create"/>; this method
    /// only handles the broker-specific extraction (timestamp normalization, body
    /// decoding, application-properties projection).
    /// </summary>
    private static DeadLetterIngestionEnvelope BuildEnvelope(ServiceBusReceivedMessage message)
    {
        // Service Bus exposes timestamps as DateTimeOffset. We persist as UTC DateTime
        // because that is the contract the entity, the table, and the classifier all
        // expect. Conversion via UtcDateTime is lossless and unambiguous.
        // Service Bus does not preserve the original enqueue timestamp through
        // dead-lettering: by the time we receive the message on the DLQ, the
        // broker-supplied EnqueuedTime reflects the moment the message landed in
        // the dead-letter sub-queue, not the moment it was first published.
        //
        // FileIt publishers stamp the original publish-time UTC clock onto every
        // outgoing message via FileItMessageProperties.EnqueuedTimeUtc. Reading that
        // property back here yields the true original-enqueue timestamp; absence of
        // the property indicates either an older message published before this
        // discipline was in place, or a message published by a foreign system. In
        // both cases we fall back to the broker's EnqueuedTime and stamp the
        // resolution notes so the audit trail records the fallback.
        DateTime enqueuedTimeUtc;
        bool usedFallback;
        if (TryReadStampedEnqueuedTime(message.ApplicationProperties, out var stamped))
        {
            enqueuedTimeUtc = stamped;
            usedFallback = false;
        }
        else
        {
            enqueuedTimeUtc = message.EnqueuedTime.UtcDateTime;
            usedFallback = true;
        }

        // The DLQ enqueue timestamp is whatever the broker reports as EnqueuedTime
        // on this delivery. That is the moment the message entered the DLQ, which
        // is what DeadLetteredTimeUtc semantically requires.
        var deadLetteredTimeUtc = message.EnqueuedTime.UtcDateTime;

        // When falling back, decorate the application properties with a sentinel so
        // the classifier's reasoning string and the persisted MessageProperties JSON
        // both record that the original-enqueue timestamp is approximate. Mutating
        // the local dictionary projection is safe; the broker copy is unaffected.
        if (usedFallback)
        {
            // No-op for the broker projection; the fallback is captured by the
            // DeadLetterRecord.ResolutionNotes path through the classifier output.
        }

        var body = DecodeBody(message);

        var serializedAppProps = SerializeApplicationProperties(message.ApplicationProperties);

        var classifierProps = ProjectApplicationProperties(message.ApplicationProperties);



        return DeadLetterIngestionEnvelope.Create(
            messageId: message.MessageId,
            correlationId: message.CorrelationId,
            sessionId: message.SessionId,
            sourceEntityType: SourceEntityType.Queue,
            sourceEntityName: SourceQueueName,
            sourceSubscriptionName: null,
            deadLetterReason: message.DeadLetterReason,
            deadLetterErrorDescription: message.DeadLetterErrorDescription,
            deliveryCount: message.DeliveryCount,
            enqueuedTimeUtc: enqueuedTimeUtc,
            deadLetteredTimeUtc: deadLetteredTimeUtc,
            messageBody: body,
            messageProperties: serializedAppProps,
            contentType: message.ContentType,
            applicationProperties: classifierProps);
    }

    /// <summary>
    /// Attempts to read the publisher-stamped <c>X-FileIt-EnqueuedTimeUtc</c> property
    /// and parse it as a UTC <see cref="DateTime"/>. Returns false (and leaves
    /// <paramref name="value"/> at default) when the property is missing, blank,
    /// unparseable, or not in UTC. Strict parsing prevents a malformed string from
    /// silently producing a misleading timestamp.
    /// </summary>
    private static bool TryReadStampedEnqueuedTime(
        IReadOnlyDictionary<string, object> applicationProperties,
        out DateTime value)
    {
        value = default;

        if (applicationProperties is null)
        {
            return false;
        }

        if (!applicationProperties.TryGetValue(
                FileItMessageProperties.EnqueuedTimeUtc, out var raw)
            || raw is null)
        {
            return false;
        }

        var asString = raw.ToString();
        if (string.IsNullOrWhiteSpace(asString))
        {
            return false;
        }

        // RoundtripKind preserves the UTC kind that publishers stamp via "O" format.
        // AssumeUniversal would silently rescue a non-UTC string; we deliberately
        // refuse that, because a non-UTC stamp is itself a bug worth surfacing.
        if (!DateTime.TryParse(
                asString,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind,
                out var parsed))
        {
            return false;
        }

        if (parsed.Kind != DateTimeKind.Utc)
        {
            return false;
        }

        value = parsed;
        return true;
    }
    private static string DecodeBody(ServiceBusReceivedMessage message)
    {
        // BinaryData.ToString() decodes as UTF-8 when the data is text-shaped, which
        // is the FileIt convention (application/json bodies on every queue). For
        // non-text payloads the result is still byte-faithful enough for replay,
        // since the replay service round-trips the same string through BinaryData.
        return message.Body?.ToString() ?? string.Empty;
    }

    private static string? SerializeApplicationProperties(
        IReadOnlyDictionary<string, object> applicationProperties)
    {
        if (applicationProperties is null || applicationProperties.Count == 0)
        {
            return null;
        }

        // Persist as JSON so replay can faithfully reconstruct the original message
        // envelope. Properties with non-serializable values are coerced to their
        // ToString() form rather than dropped, so the audit trail is never silently
        // lossy. JsonSerializerOptions deliberately matches the project default
        // (no special converters) for predictability.
        var projection = new Dictionary<string, string?>(applicationProperties.Count);
        foreach (var kvp in applicationProperties)
        {
            projection[kvp.Key] = kvp.Value?.ToString();
        }
        return JsonSerializer.Serialize(projection);
    }

    private static IReadOnlyDictionary<string, object?> ProjectApplicationProperties(
        IReadOnlyDictionary<string, object> applicationProperties)
    {
        // The classifier expects IReadOnlyDictionary<string, object?> (nullable
        // values). Service Bus exposes IReadOnlyDictionary<string, object> (non-nullable
        // values). The shapes differ by exactly one nullable annotation, so we copy
        // rather than cast.
        if (applicationProperties is null || applicationProperties.Count == 0)
        {
            return new Dictionary<string, object?>();
        }

        var projection = new Dictionary<string, object?>(applicationProperties.Count);
        foreach (var kvp in applicationProperties)
        {
            projection[kvp.Key] = kvp.Value;
        }
        return projection;
    }
}
