using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Infrastructure.Classification;

/// <summary>
/// Assigns a <see cref="FailureCategory"/> to a dead-lettered Service Bus message.
/// Implementations must be pure: no I/O, no clock reads, no logging. Classification
/// must be deterministic for a given input so historical re-classification produces
/// stable results.
/// </summary>
public interface IDeadLetterClassifier
{
    /// <summary>
    /// Classify a dead-lettered message. Never returns null; falls through to
    /// <see cref="FailureCategory.Unknown"/> with an explanatory reasoning string
    /// when no rule matches confidently.
    /// </summary>
    DeadLetterClassification Classify(DeadLetterClassificationInput input);
}
