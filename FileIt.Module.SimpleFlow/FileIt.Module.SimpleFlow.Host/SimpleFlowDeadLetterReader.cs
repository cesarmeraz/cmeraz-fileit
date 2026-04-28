// Reader function for the api-add-simple-sub subscription's dead-letter sub-queue.
// Listens on api-add-topic/Subscriptions/api-add-simple-sub/$DeadLetterQueue,
// hands each dead-lettered message to the shared IDeadLetterIngestionService for
// classification + persistence, and completes the DLQ message so the DLQ itself
// does not fill up.
//
// This is the only DLQ reader in FileIt that targets a topic subscription rather
// than a queue. SourceEntityType.Topic + SourceSubscriptionName drive the
// subscription-presence invariant on DeadLetterRecord.
//
// See docs/dead-letter-strategy.md for the full design.
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.DeadLetter;
using FileIt.Infrastructure;
using FileIt.Infrastructure.DeadLetter.Ingestion;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.SimpleFlow.Host;

/// <summary>
/// Azure Function that drains the api-add-simple-sub subscription's dead-letter
/// sub-queue on the api-add-topic.
/// </summary>
/// <remarks>
/// <para>
/// Adapter only. Builds a <see cref="DeadLetterIngestionEnvelope"/> from the
/// trigger arguments and delegates to <see cref="IDeadLetterIngestionService"/>.
/// </para>
/// <para>
/// The trigger path uses the documented Azure Service Bus subscription
/// dead-letter syntax <c>topic/Subscriptions/subscription/$deadletterqueue</c>.
/// Subscriptions on the same topic each have their own DLQ, so a future second
/// subscription would get its own reader function rather than sharing this one.
/// </para>
/// </remarks>
public class SimpleFlowDeadLetterReader
{
    public const string TopicName = "api-add-topic";
    public const string SubscriptionName = "api-add-simple-sub";
    public const string DeadLetterPath =
        TopicName + "/Subscriptions/" + SubscriptionName + "/$deadletterqueue";

    private readonly IDeadLetterIngestionService _ingestion;
    private readonly ILogger<SimpleFlowDeadLetterReader> _logger;

    public SimpleFlowDeadLetterReader(
        IDeadLetterIngestionService ingestion,
        ILogger<SimpleFlowDeadLetterReader> logger)
    {
        _ingestion = ingestion ?? throw new ArgumentNullException(nameof(ingestion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(nameof(SimpleFlowDeadLetterReader))]
    public async Task Run(
        [ServiceBusTrigger(TopicName, SubscriptionName + "/$deadletterqueue")]
            ServiceBusReceivedMessage message,
        FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(message);
        var cancellationToken = context.CancellationToken;
        var correlationId = message.CorrelationId ?? string.Empty;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "DeadLetterSource", DeadLetterPath },
        }))
        {
            _logger.LogInformation(
                InfrastructureEvents.DeadLetterMessageReceived,
                "Received dead-letter from {TopicName}/{SubscriptionName} "
                    + "(MessageId={MessageId}, DeliveryCount={DeliveryCount}, "
                    + "Reason={DeadLetterReason}).",
                TopicName,
                SubscriptionName,
                message.MessageId,
                message.DeliveryCount,
                message.DeadLetterReason ?? "<null>");

            var envelope = BuildEnvelope(message);

            var record = await _ingestion.IngestAsync(envelope, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation(
                InfrastructureEvents.DeadLetterRecordPersisted,
                "Dead-letter ingestion complete for {TopicName}/{SubscriptionName} "
                    + "MessageId={MessageId}; DeadLetterRecordId={DeadLetterRecordId}, "
                    + "Category={FailureCategory}.",
                TopicName,
                SubscriptionName,
                message.MessageId,
                record.DeadLetterRecordId,
                record.FailureCategory);
        }
    }

    private static DeadLetterIngestionEnvelope BuildEnvelope(ServiceBusReceivedMessage message)
    {
        // See DataFlowDeadLetterReader for the full timestamp-discipline rationale.
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
            sourceEntityType: SourceEntityType.Topic,
            sourceEntityName: TopicName,
            sourceSubscriptionName: SubscriptionName,
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