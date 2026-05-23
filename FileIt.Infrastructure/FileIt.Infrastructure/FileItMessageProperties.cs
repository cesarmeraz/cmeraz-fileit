namespace FileIt.Infrastructure;

/// <summary>
/// Application-property names FileIt stamps on every Service Bus message it publishes,
/// and reads back from every dead-letter receive. The constants are the contract
/// between publishers (<c>BusTool</c>, <c>PublishTool</c>) and readers
/// (<c>DataFlowDeadLetterReader</c> and siblings).
/// </summary>
/// <remarks>
/// <para>
/// The <c>X-FileIt-</c> prefix marks these as application-level metadata distinct from
/// Service Bus broker properties. Renaming any constant requires a coordinated change
/// to every publisher and every reader; the strings are persisted into
/// <c>dbo.DeadLetterRecord.MessageProperties</c> as JSON, so renames also affect the
/// audit trail.
/// </para>
/// </remarks>
public static class FileItMessageProperties
{
    /// <summary>
    /// UTC timestamp at which the publisher constructed the message and handed it to
    /// the Service Bus client. Round-tripped as ISO 8601 with the <c>O</c> format
    /// specifier so parsing is unambiguous and timezone-safe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Service Bus does not preserve the original enqueue timestamp through
    /// dead-lettering: the <c>EnqueuedTime</c> on a DLQ-delivered message reflects
    /// when it landed in the dead-letter sub-queue, not when it was first published
    /// to the source queue. Stamping this property at publish time is the only
    /// reliable way for downstream observability (and especially the
    /// <c>DeadLetterRecord.EnqueuedTimeUtc</c> column) to reflect the true age of
    /// the failure rather than the age of the dead-letter event.
    /// </para>
    /// <para>
    /// Readers must tolerate the property's absence (older messages, foreign
    /// publishers) and document the fallback in their resolution notes.
    /// </para>
    /// </remarks>
    public const string EnqueuedTimeUtc = "X-FileIt-EnqueuedTimeUtc";
}