namespace FileIt.Domain.Entities.DeadLetter;

/// <summary>
/// Kind of Service Bus entity a dead-lettered message came from.
/// </summary>
/// <remarks>
/// Persisted to <c>dbo.DeadLetterRecord.SourceEntityType</c> as the string name via
/// an EF value converter. The database enforces the same set via
/// <c>CK_DeadLetterRecord_SourceEntityType</c>, and the related
/// <c>CK_DeadLetterRecord_SubscriptionPresence</c> invariant requires
/// <c>SourceSubscriptionName</c> to be null for <see cref="Queue"/> rows and non-null
/// for <see cref="Topic"/> rows.
/// </remarks>
public enum SourceEntityType
{
    /// <summary>
    /// Message was dead-lettered from a Service Bus queue.
    /// <see cref="DeadLetterRecord.SourceSubscriptionName"/> must be null.
    /// </summary>
    Queue = 0,

    /// <summary>
    /// Message was dead-lettered from a subscription on a Service Bus topic.
    /// <see cref="DeadLetterRecord.SourceSubscriptionName"/> must identify the
    /// subscription and cannot be null.
    /// </summary>
    Topic = 1,
}
