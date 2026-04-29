// Reader function for the api-add-fileitmodule-sub subscription's dead-letter sub-queue.
// Drains api-add-topic/Subscriptions/api-add-fileitmodule-sub/$DeadLetterQueue,
// hands each dead-lettered message to IDeadLetterIngestionService, and completes
// the DLQ message so the DLQ itself does not fill up.
//
// See docs/dead-letter-strategy.md for the full design.
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.DeadLetter;
using FileIt.Infrastructure;
using FileIt.Infrastructure.DeadLetter.Ingestion;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.FileItModule.Host;

public class FileItModuleDeadLetterReader
{
    public const string TopicName = "api-add-topic";
    public const string SubscriptionName = "api-add-fileitmodule-sub";
    public const string DeadLetterPath =
        TopicName + "/Subscriptions/" + SubscriptionName + "/$deadletterqueue";

    private readonly IDeadLetterIngestionService _ingestion;
    private readonly ILogger<FileItModuleDeadLetterReader> _logger;

    public FileItModuleDeadLetterReader(
        IDeadLetterIngestionService ingestion,
        ILogger<FileItModuleDeadLetterReader> logger)
    {
        _ingestion = ingestion ?? throw new ArgumentNullException(nameof(ingestion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(nameof(FileItModuleDeadLetterReader))]
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
