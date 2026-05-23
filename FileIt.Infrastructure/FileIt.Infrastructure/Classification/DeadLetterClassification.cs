using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Infrastructure.Classification;

/// <summary>
/// Result of classifying a dead-lettered message.
/// The <see cref="Reasoning"/> string is persisted into
/// <c>dbo.DeadLetterRecord.ResolutionNotes</c> on insert so operators opening a row
/// immediately see <em>why</em> the classifier chose this category without having to
/// re-run the rule set themselves.
/// </summary>
/// <param name="Category">The chosen <see cref="FailureCategory"/>.</param>
/// <param name="Reasoning">
/// Short, human-readable explanation of which rule fired and what signals it matched.
/// Example: "DeadLetterReason 'MaxDeliveryCountExceeded' + no transient exception signal in
/// description '...' -> treated as Poison (handler rejects every delivery)."
/// Kept short; this lands in a log message and a SQL column, not a wiki page.
/// </param>
/// <param name="MatchedRule">
/// Identifier of the rule that fired, for audit and rule-performance analysis.
/// Stable across releases; renames require a classifier-version bump.
/// </param>
public sealed record DeadLetterClassification(
    FailureCategory Category,
    string Reasoning,
    string MatchedRule);
