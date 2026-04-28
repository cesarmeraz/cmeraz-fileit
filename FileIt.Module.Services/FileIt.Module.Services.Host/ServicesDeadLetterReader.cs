// Reader function for the api-add queue's dead-letter sub-queue.
// Listens on api-add/$DeadLetterQueue, hands each dead-lettered message to the
// shared IDeadLetterIngestionService for classification + persistence, and
// completes the DLQ message so the DLQ itself does not fill up.
//
// Mirrors DataFlowDeadLetterReader exactly. Differences are limited to the
// trigger path and the source entity name; all real logic lives in the shared
// ingestion service.
//
// See docs/dead-letter-strategy.md for the full design.
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.DeadLetter;
using FileIt.Infrastructure;
using FileIt.Infrastructure.DeadLetter.Ingestion;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Services.Host;

/// <summary>
/// Azure Function that drains the api-add queue's dead-letter sub-queue.
/// </summary>
/// <remarks>
/// <para>
/// Adapter only. Builds a <see cref="DeadLetterIngestionEnvelope"/> from the
/// trigger arguments and delegates to <see cref="IDeadLetterIngestionService"/>.
/// All cross-channel concerns (classification, idempotency, audit logging) live
/// behind that interface.
/// </para>
/// </remarks>
public class ServicesDeadLetterReader
{
    public const string DeadLetterPath = "api-add/$deadletterqueue";
    public const string SourceQueueName = "api-add";

    private readonly IDeadLetterIngestionService _ingestion;
    private readonly ILogger<ServicesDeadLetterReader> _logger;

    public ServicesDeadLetterReader(
        IDeadLetterIngestionService ingestion,
        ILogger<ServicesDeadLetterReader> logger)
    {
        _ingestion = ingestion ?? throw new ArgumentNullException(nameof(ingestion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(nameof(ServicesDeadLetterReader))]
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

    private static DeadLetterIngestionEnvelope BuildEnvelope(ServiceBusReceivedMessage message)
    {
        // See DataFlowDeadLetterReader for the full timestamp-discipline rationale:
        // Service Bus does not preserve original-enqueue timestamps through
        // dead-lettering, so FileIt publishers stamp the publish-time UTC clock
        // onto every outgoing message. We prefer that stamp; fall back to the
        // broker's EnqueuedTime when absent.
        DateTime enqueuedTimeUtc;
        if (TryReadStampedEnqueuedTime(message.ApplicationProperties, out var stamped))
        {
            enqueuedTimeUtc = stamped;
        }
        else
        {
            enqueuedTimeUtc = message.EnqueuedTime.UtcDateTime;
        }

        var deadLetteredTimeUtc = message.EnqueuedTime.UtcDateTime;

        var body = message.Body?.ToString() ?? string.Empty;

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

    private static string? SerializeApplicationProperties(
        IReadOnlyDictionary<string, object> applicationProperties)
    {
        if (applicationProperties is null || applicationProperties.Count == 0)
        {
            return null;
        }

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
}