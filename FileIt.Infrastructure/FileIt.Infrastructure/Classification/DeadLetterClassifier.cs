using FileIt.Domain.Entities.DeadLetter;

namespace FileIt.Infrastructure.Classification;

/// <summary>
/// Default rule-based classifier. Pure function of <see cref="DeadLetterClassificationInput"/>;
/// no I/O, no logging, no clock access. Safe to invoke from readers, from replay
/// decision paths, and from historical re-classification jobs.
/// </summary>
/// <remarks>
/// <para>
/// Rules evaluate in the order declared in <see cref="Classify"/>. Order matters:
/// more specific signals must win over more general ones. Explicit hints set by a
/// handler beat Service Bus's own reason string; Service Bus's reason string beats
/// heuristic pattern matches on the description; heuristic matches beat the
/// catch-all.
/// </para>
/// <para>
/// Rules are deliberately narrow. When in doubt, fall through to
/// <see cref="FailureCategory.Unknown"/>; a confident operator review is safer than
/// a confident wrong category.
/// </para>
/// <para>
/// See docs/dead-letter-strategy.md Section 4 for the taxonomy.
/// </para>
/// </remarks>
public sealed class DeadLetterClassifier : IDeadLetterClassifier
{
    /// <summary>
    /// Application property name handlers may set to hint the category explicitly.
    /// Value must be one of the <see cref="FailureCategory"/> names (case-insensitive).
    /// Invalid values are ignored; the classifier falls through to the next rule.
    /// </summary>
    public const string ExplicitCategoryPropertyName = "X-FileIt-FailureCategory";

    /// <summary>
    /// Application property name handlers may set to mark a message as a deliberate
    /// poison trigger. Any truthy value (presence is sufficient) classifies as
    /// <see cref="FailureCategory.Poison"/>. See
    /// docs/dead-letter-strategy.md Section 10.
    /// </summary>
    public const string PoisonMarkerPropertyName = "X-FileIt-Poison";

    /// <summary>
    /// Prefix convention for poison triggers embedded in payloads (e.g. a CSV GL Account
    /// value of <c>POISON_FORCE_DEADLETTER</c>). Surfaced via
    /// <see cref="DeadLetterClassificationInput.DeadLetterErrorDescription"/> when the
    /// validator rejects the row.
    /// </summary>
    public const string PoisonPayloadPrefix = "POISON_";

    // Service Bus built-in reason strings. Azure.Messaging.ServiceBus exposes some of these
    // via DeadLetterReason.* constants but the strings are the stable contract and what
    // actually appears on the wire.
    private const string ReasonMaxDeliveryCountExceeded = "MaxDeliveryCountExceeded";
    private const string ReasonTtlExpired = "TTLExpiredException";
    private const string ReasonSessionLockLost = "SessionLockLost";
    private const string ReasonHeaderSizeExceeded = "HeaderSizeExceeded";

    public DeadLetterClassification Classify(DeadLetterClassificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Rule 1: explicit handler hint wins over everything.
        if (TryClassifyByExplicitHint(input) is { } explicit_)
        {
            return explicit_;
        }

        // Rule 2: deliberate poison marker.
        if (TryClassifyByPoisonMarker(input) is { } markerResult)
        {
            return markerResult;
        }

        // Rule 3: poison payload prefix embedded in the error description.
        if (TryClassifyByPoisonPayloadPrefix(input) is { } prefixResult)
        {
            return prefixResult;
        }

        // Rule 4: Service Bus built-in reason strings.
        if (TryClassifyByBuiltInReason(input) is { } builtInResult)
        {
            return builtInResult;
        }

        // Rule 5: heuristic pattern match on the error description.
        if (TryClassifyByDescriptionHeuristic(input) is { } heuristicResult)
        {
            return heuristicResult;
        }

        // Catch-all: no rule matched with confidence. Escalate for human review.
        return new DeadLetterClassification(
            Category: FailureCategory.Unknown,
            Reasoning: BuildReasoning(
                "no classifier rule matched",
                $"Reason='{input.DeadLetterReason ?? "<null>"}', "
                + $"Description='{Truncate(input.DeadLetterErrorDescription, 120)}'",
                input),
            MatchedRule: "CatchAll_Unknown");
    }

    private static DeadLetterClassification? TryClassifyByExplicitHint(
        DeadLetterClassificationInput input)
    {
        if (!input.ApplicationProperties.TryGetValue(ExplicitCategoryPropertyName, out var raw)
            || raw is null)
        {
            return null;
        }

        var asString = raw.ToString();
        if (string.IsNullOrWhiteSpace(asString))
        {
            return null;
        }

        if (!Enum.TryParse<FailureCategory>(asString, ignoreCase: true, out var parsed))
        {
            return null;
        }

        return new DeadLetterClassification(
            Category: parsed,
            Reasoning: BuildReasoning(
                $"handler set explicit hint {ExplicitCategoryPropertyName}='{asString}'",
                $"parsed as FailureCategory.{parsed}",
                input),
            MatchedRule: "ExplicitHint");
    }

    private static DeadLetterClassification? TryClassifyByPoisonMarker(
        DeadLetterClassificationInput input)
    {
        if (!input.ApplicationProperties.ContainsKey(PoisonMarkerPropertyName))
        {
            return null;
        }

        return new DeadLetterClassification(
            Category: FailureCategory.Poison,
            Reasoning: BuildReasoning(
                $"application property {PoisonMarkerPropertyName} present",
                "classified as deliberate poison trigger",
                input),
            MatchedRule: "PoisonMarker");
    }

    private static DeadLetterClassification? TryClassifyByPoisonPayloadPrefix(
        DeadLetterClassificationInput input)
    {
        var description = input.DeadLetterErrorDescription;
        if (string.IsNullOrEmpty(description))
        {
            return null;
        }

        if (description.IndexOf(PoisonPayloadPrefix, StringComparison.Ordinal) < 0)
        {
            return null;
        }

        return new DeadLetterClassification(
            Category: FailureCategory.Poison,
            Reasoning: BuildReasoning(
                $"error description contains poison prefix '{PoisonPayloadPrefix}'",
                $"description snippet: '{Truncate(description, 120)}'",
                input),
            MatchedRule: "PoisonPayloadPrefix");
    }

    private static DeadLetterClassification? TryClassifyByBuiltInReason(
        DeadLetterClassificationInput input)
    {
        var reason = input.DeadLetterReason;
        if (string.IsNullOrEmpty(reason))
        {
            return null;
        }

        if (string.Equals(reason, ReasonMaxDeliveryCountExceeded, StringComparison.Ordinal))
        {
            return new DeadLetterClassification(
                Category: FailureCategory.Poison,
                Reasoning: BuildReasoning(
                    $"Service Bus reason '{ReasonMaxDeliveryCountExceeded}'",
                    $"handler failed all {input.DeliveryCount} deliveries; treating as "
                    + "Poison pending operator review (cluster patterns may be "
                    + "reclassified to DownstreamUnavailable at the reporting layer)",
                    input),
                MatchedRule: "BuiltInReason_MaxDeliveryCountExceeded");
        }

        if (string.Equals(reason, ReasonTtlExpired, StringComparison.Ordinal))
        {
            return new DeadLetterClassification(
                Category: FailureCategory.DownstreamUnavailable,
                Reasoning: BuildReasoning(
                    $"Service Bus reason '{ReasonTtlExpired}'",
                    "message aged out before consumption; downstream was unavailable long "
                    + "enough that the TTL elapsed",
                    input),
                MatchedRule: "BuiltInReason_TTLExpired");
        }

        if (string.Equals(reason, ReasonSessionLockLost, StringComparison.Ordinal))
        {
            return new DeadLetterClassification(
                Category: FailureCategory.Transient,
                Reasoning: BuildReasoning(
                    $"Service Bus reason '{ReasonSessionLockLost}'",
                    "session lock lost is a Service Bus-side transient condition",
                    input),
                MatchedRule: "BuiltInReason_SessionLockLost");
        }

        if (string.Equals(reason, ReasonHeaderSizeExceeded, StringComparison.Ordinal))
        {
            return new DeadLetterClassification(
                Category: FailureCategory.SchemaViolation,
                Reasoning: BuildReasoning(
                    $"Service Bus reason '{ReasonHeaderSizeExceeded}'",
                    "publisher emitted a message exceeding header size limits; upstream "
                    + "contract violation",
                    input),
                MatchedRule: "BuiltInReason_HeaderSizeExceeded");
        }

        return null;
    }

    private static DeadLetterClassification? TryClassifyByDescriptionHeuristic(
        DeadLetterClassificationInput input)
    {
        var description = input.DeadLetterErrorDescription;
        if (string.IsNullOrEmpty(description))
        {
            return null;
        }

        var schemaMarkers = new[]
        {
            "JsonException",
            "JsonReaderException",
            "Unexpected character",
            "Invalid JSON",
            "Required property",
            "could not be deserialized",
            "ValidationException",
            "FormatException",
            "CsvHelperException",
            "malformed",
        };
        foreach (var marker in schemaMarkers)
        {
            if (description.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new DeadLetterClassification(
                    Category: FailureCategory.SchemaViolation,
                    Reasoning: BuildReasoning(
                        $"description contains schema-violation marker '{marker}'",
                        $"description snippet: '{Truncate(description, 120)}'",
                        input),
                    MatchedRule: $"Heuristic_Schema_{marker}");
            }
        }

        var transientMarkers = new[]
        {
            "TimeoutException",
            "OperationCanceledException",
            "TaskCanceledException",
            "transient",
            "deadlock victim",
            "connection was forcibly closed",
            "SocketException",
        };
        foreach (var marker in transientMarkers)
        {
            if (description.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new DeadLetterClassification(
                    Category: FailureCategory.Transient,
                    Reasoning: BuildReasoning(
                        $"description contains transient marker '{marker}'",
                        $"description snippet: '{Truncate(description, 120)}'",
                        input),
                    MatchedRule: $"Heuristic_Transient_{marker}");
            }
        }

        var downstreamMarkers = new[]
        {
            "No such host is known",
            "Connection refused",
            "503 Service Unavailable",
            "HttpRequestException: Name or service not known",
            "SqlException: A network-related or instance-specific error",
        };
        foreach (var marker in downstreamMarkers)
        {
            if (description.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new DeadLetterClassification(
                    Category: FailureCategory.DownstreamUnavailable,
                    Reasoning: BuildReasoning(
                        $"description contains downstream-unavailable marker '{marker}'",
                        $"description snippet: '{Truncate(description, 120)}'",
                        input),
                    MatchedRule: $"Heuristic_Downstream_{marker}");
            }
        }

        return null;
    }

    private static string BuildReasoning(
        string primary,
        string detail,
        DeadLetterClassificationInput input)
    {
        return $"{primary}. {detail}. "
            + $"Source='{input.SourceEntityName}', DeliveryCount={input.DeliveryCount}.";
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= max ? value : value[..max] + "...";
    }
}
