using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Infrastructure.Classification;

/// <summary>
/// All the signals the classifier reads from a dead-lettered message.
/// Deliberately decoupled from <c>Azure.Messaging.ServiceBus.ServiceBusReceivedMessage</c>
/// so the classifier can be unit tested without a Service Bus client, and so the same
/// classifier runs against persisted <see cref="DeadLetterRecord"/> rows if we ever
/// re-classify historical rows under new rules.
/// </summary>
/// <param name="DeadLetterReason">
/// The <c>DeadLetterReason</c> application property set by Service Bus (or by an
/// explicit dead-letter call from the handler). Null if absent. Primary classification signal.
/// </param>
/// <param name="DeadLetterErrorDescription">
/// The <c>DeadLetterErrorDescription</c> application property. Usually the exception
/// message or a short diagnostic string. Null if absent.
/// </param>
/// <param name="DeliveryCount">
/// Number of delivery attempts before dead-lettering. Non-negative by contract;
/// enforced at the database via <c>CK_DeadLetterRecord_DeliveryCount_NonNegative</c>.
/// </param>
/// <param name="SourceEntityName">
/// The queue or topic name the message was dead-lettered from. Used for diagnostic
/// context in <see cref="DeadLetterClassification.Reasoning"/>.
/// </param>
/// <param name="ApplicationProperties">
/// All additional application properties on the message envelope, keyed by name.
/// Classifier inspects these for channel-specific poison markers and for explicit
/// category hints (<c>X-FileIt-FailureCategory</c>) set by handlers that already know
/// why they are rejecting a message.
/// </param>
public sealed record DeadLetterClassificationInput(
    string? DeadLetterReason,
    string? DeadLetterErrorDescription,
    int DeliveryCount,
    string SourceEntityName,
    IReadOnlyDictionary<string, object?> ApplicationProperties);
