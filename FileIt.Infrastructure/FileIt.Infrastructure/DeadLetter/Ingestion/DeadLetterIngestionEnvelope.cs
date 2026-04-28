using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Infrastructure.DeadLetter.Ingestion;

/// <summary>
/// Validated, broker-agnostic representation of a dead-lettered message ready for ingestion.
/// </summary>
/// <remarks>
/// <para>
/// Constructing this record is the single point at which raw broker data crosses into
/// the FileIt domain. Once an envelope exists, every downstream component
/// (<see cref="IDeadLetterIngestionService"/>, the classifier, the repo) may trust
/// that required fields are present and well-formed, that
/// <see cref="SubscriptionPresenceInvariant"/> holds, and that timestamps are UTC.
/// </para>
/// <para>
/// Deliberately decoupled from <c>Azure.Messaging.ServiceBus.ServiceBusReceivedMessage</c>
/// so the ingestion path is testable without a Service Bus client and so the same
/// path could ingest historical records read from a backup or a future
/// re-classification job.
/// </para>
/// <para>
/// This record carries no behavior. Validation is in <see cref="Create"/>; construction
/// via the primary constructor is permitted but bypasses the invariants. Production
/// code paths must use <see cref="Create"/>.
/// </para>
/// </remarks>
public sealed record DeadLetterIngestionEnvelope
{
    /// <summary>Service Bus MessageId of the dead-lettered message. Required, non-empty.</summary>
    public required string MessageId { get; init; }

    /// <summary>Correlation id used to join against CommonLog. Null if the publisher did not set one.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Session id when the source channel is session-enabled. Null otherwise.</summary>
    public string? SessionId { get; init; }

    /// <summary>Whether the source was a queue or a topic subscription.</summary>
    public required SourceEntityType SourceEntityType { get; init; }

    /// <summary>Queue or topic name. Required, non-empty.</summary>
    public required string SourceEntityName { get; init; }

    /// <summary>
    /// Subscription name. Non-null iff <see cref="SourceEntityType"/> is
    /// <see cref="SourceEntityType.Topic"/>; null iff Queue. Enforced by
    /// <see cref="SubscriptionPresenceInvariant"/>.
    /// </summary>
    public string? SourceSubscriptionName { get; init; }

    /// <summary>Service Bus DeadLetterReason application property. May be null.</summary>
    public string? DeadLetterReason { get; init; }

    /// <summary>Service Bus DeadLetterErrorDescription application property. May be null.</summary>
    public string? DeadLetterErrorDescription { get; init; }

    /// <summary>Number of delivery attempts before dead-lettering. Non-negative.</summary>
    public required int DeliveryCount { get; init; }

    /// <summary>When the original message was first enqueued, in UTC. Required.</summary>
    public required DateTime EnqueuedTimeUtc { get; init; }

    /// <summary>When the message was moved to the dead-letter sub-queue, in UTC. Required.</summary>
    public required DateTime DeadLetteredTimeUtc { get; init; }

    /// <summary>Verbatim message body as the reader received it. Required.</summary>
    public required string MessageBody { get; init; }

    /// <summary>Application properties on the message envelope, JSON-serialized. Null if none.</summary>
    public string? MessageProperties { get; init; }

    /// <summary>Service Bus ContentType property. Null if absent.</summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// All application properties on the original message, used by the classifier.
    /// Distinct from <see cref="MessageProperties"/>, which is the JSON snapshot
    /// persisted to the database for replay reconstruction.
    /// </summary>
    public required IReadOnlyDictionary<string, object?> ApplicationProperties { get; init; }

    /// <summary>
    /// Constructs an envelope and validates the invariants. Use this in production code paths.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when a required field is missing, when DeliveryCount is negative,
    /// when timestamps are not UTC, or when the subscription presence invariant is violated.
    /// </exception>
    public static DeadLetterIngestionEnvelope Create(
        string messageId,
        string? correlationId,
        string? sessionId,
        SourceEntityType sourceEntityType,
        string sourceEntityName,
        string? sourceSubscriptionName,
        string? deadLetterReason,
        string? deadLetterErrorDescription,
        int deliveryCount,
        DateTime enqueuedTimeUtc,
        DateTime deadLetteredTimeUtc,
        string messageBody,
        string? messageProperties,
        string? contentType,
        IReadOnlyDictionary<string, object?> applicationProperties)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException(
                "MessageId is required.", nameof(messageId));
        }
        if (string.IsNullOrWhiteSpace(sourceEntityName))
        {
            throw new ArgumentException(
                "SourceEntityName is required.", nameof(sourceEntityName));
        }
        if (deliveryCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deliveryCount), deliveryCount,
                "DeliveryCount must be non-negative.");
        }
        if (enqueuedTimeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "EnqueuedTimeUtc must be UTC.", nameof(enqueuedTimeUtc));
        }
        if (deadLetteredTimeUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "DeadLetteredTimeUtc must be UTC.", nameof(deadLetteredTimeUtc));
        }
        if (messageBody is null)
        {
            throw new ArgumentNullException(
                nameof(messageBody),
                "MessageBody is required; replay is impossible without the body.");
        }
        ArgumentNullException.ThrowIfNull(applicationProperties);

        SubscriptionPresenceInvariant(sourceEntityType, sourceSubscriptionName);

        return new DeadLetterIngestionEnvelope
        {
            MessageId = messageId,
            CorrelationId = NullIfBlank(correlationId),
            SessionId = NullIfBlank(sessionId),
            SourceEntityType = sourceEntityType,
            SourceEntityName = sourceEntityName,
            SourceSubscriptionName = NullIfBlank(sourceSubscriptionName),
            DeadLetterReason = NullIfBlank(deadLetterReason),
            DeadLetterErrorDescription = deadLetterErrorDescription,
            DeliveryCount = deliveryCount,
            EnqueuedTimeUtc = enqueuedTimeUtc,
            DeadLetteredTimeUtc = deadLetteredTimeUtc,
            MessageBody = messageBody,
            MessageProperties = messageProperties,
            ContentType = NullIfBlank(contentType),
            ApplicationProperties = applicationProperties,
        };
    }

    /// <summary>
    /// Mirrors <c>CK_DeadLetterRecord_SubscriptionPresence</c>: SourceSubscriptionName
    /// must be null for queues and non-null for topics. Catching this in code keeps
    /// failures out of the database round-trip and produces a clean error message.
    /// </summary>
    private static void SubscriptionPresenceInvariant(
        SourceEntityType sourceEntityType,
        string? sourceSubscriptionName)
    {
        switch (sourceEntityType)
        {
            case SourceEntityType.Queue:
                if (!string.IsNullOrWhiteSpace(sourceSubscriptionName))
                {
                    throw new ArgumentException(
                        "SourceSubscriptionName must be null when SourceEntityType is Queue.",
                        nameof(sourceSubscriptionName));
                }
                break;

            case SourceEntityType.Topic:
                if (string.IsNullOrWhiteSpace(sourceSubscriptionName))
                {
                    throw new ArgumentException(
                        "SourceSubscriptionName is required when SourceEntityType is Topic.",
                        nameof(sourceSubscriptionName));
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(sourceEntityType),
                    sourceEntityType,
                    "Unknown SourceEntityType.");
        }
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}