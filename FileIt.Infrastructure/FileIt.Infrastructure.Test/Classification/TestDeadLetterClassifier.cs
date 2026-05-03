using FileIt.Domain.Entities.DeadLetter;
using FileIt.Infrastructure.Classification;

namespace FileIt.Infrastructure.Test.Classification;

[TestClass]
public class TestDeadLetterClassifier
{
    private DeadLetterClassifier _target = null!;

    [TestInitialize]
    public void Setup()
    {
        _target = new DeadLetterClassifier();
    }

    private static DeadLetterClassificationInput Build(
        string? reason = null,
        string? description = null,
        int deliveryCount = 5,
        string sourceEntity = "dataflow-transform",
        IReadOnlyDictionary<string, object?>? appProps = null)
    {
        return new DeadLetterClassificationInput(
            DeadLetterReason: reason,
            DeadLetterErrorDescription: description,
            DeliveryCount: deliveryCount,
            SourceEntityName: sourceEntity,
            ApplicationProperties: appProps ?? new Dictionary<string, object?>());
    }

    // ---- Rule 1: explicit hint ----

    [TestMethod]
    public void Classify_ExplicitHintTransient_ReturnsTransient()
    {
        var input = Build(appProps: new Dictionary<string, object?>
        {
            [DeadLetterClassifier.ExplicitCategoryPropertyName] = "Transient",
        });

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Transient, result.Category);
        Assert.AreEqual("ExplicitHint", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_ExplicitHintCaseInsensitive_StillParses()
    {
        var input = Build(appProps: new Dictionary<string, object?>
        {
            [DeadLetterClassifier.ExplicitCategoryPropertyName] = "POISON",
        });

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Poison, result.Category);
        Assert.AreEqual("ExplicitHint", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_ExplicitHintInvalidValue_FallsThroughToNextRule()
    {
        // Invalid hint should NOT match ExplicitHint rule. With no other signal,
        // should fall all the way to CatchAll_Unknown.
        var input = Build(appProps: new Dictionary<string, object?>
        {
            [DeadLetterClassifier.ExplicitCategoryPropertyName] = "GarbageValue",
        });

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Unknown, result.Category);
        Assert.AreEqual("CatchAll_Unknown", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_ExplicitHintWinsOverEverythingElse()
    {
        // Even with poison marker AND poison reason set, explicit hint should dominate.
        var input = Build(
            reason: "MaxDeliveryCountExceeded",
            appProps: new Dictionary<string, object?>
            {
                [DeadLetterClassifier.ExplicitCategoryPropertyName] = "Transient",
                [DeadLetterClassifier.PoisonMarkerPropertyName] = true,
            });

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Transient, result.Category);
        Assert.AreEqual("ExplicitHint", result.MatchedRule);
    }

    // ---- Rule 2: poison marker ----

    [TestMethod]
    public void Classify_PoisonMarkerPresent_ReturnsPoison()
    {
        var input = Build(appProps: new Dictionary<string, object?>
        {
            [DeadLetterClassifier.PoisonMarkerPropertyName] = true,
        });

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Poison, result.Category);
        Assert.AreEqual("PoisonMarker", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_PoisonMarkerWithFalsyValue_StillClassifiesAsPoison()
    {
        // Per spec: presence is sufficient. The classifier doesn't read the value.
        var input = Build(appProps: new Dictionary<string, object?>
        {
            [DeadLetterClassifier.PoisonMarkerPropertyName] = false,
        });

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Poison, result.Category);
        Assert.AreEqual("PoisonMarker", result.MatchedRule);
    }

    // ---- Rule 3: poison payload prefix in description ----

    [TestMethod]
    public void Classify_PoisonPayloadPrefixInDescription_ReturnsPoison()
    {
        var input = Build(description: "Validation failed: row 5 had POISON_FORCE_DEADLETTER");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Poison, result.Category);
        Assert.AreEqual("PoisonPayloadPrefix", result.MatchedRule);
    }

    // ---- Rule 4: built-in reasons ----

    [TestMethod]
    public void Classify_MaxDeliveryCountExceeded_ReturnsPoison()
    {
        var input = Build(reason: "MaxDeliveryCountExceeded");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Poison, result.Category);
        Assert.AreEqual("BuiltInReason_MaxDeliveryCountExceeded", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_TtlExpired_ReturnsDownstreamUnavailable()
    {
        var input = Build(reason: "TTLExpiredException");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.DownstreamUnavailable, result.Category);
        Assert.AreEqual("BuiltInReason_TTLExpired", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_SessionLockLost_ReturnsTransient()
    {
        var input = Build(reason: "SessionLockLost");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Transient, result.Category);
        Assert.AreEqual("BuiltInReason_SessionLockLost", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_HeaderSizeExceeded_ReturnsSchemaViolation()
    {
        var input = Build(reason: "HeaderSizeExceeded");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.SchemaViolation, result.Category);
        Assert.AreEqual("BuiltInReason_HeaderSizeExceeded", result.MatchedRule);
    }

    [TestMethod]
    public void Classify_BuiltInReasonIsCaseSensitive()
    {
        // Per implementation: StringComparison.Ordinal. Lowercase should NOT match.
        var input = Build(reason: "maxdeliverycountexceeded");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Unknown, result.Category);
    }

    // ---- Rule 5: heuristic markers ----

    [TestMethod]
    public void Classify_DescriptionHasJsonException_ReturnsSchemaViolation()
    {
        var input = Build(description: "JsonException: Unexpected character at line 1");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.SchemaViolation, result.Category);
        StringAssert.StartsWith(result.MatchedRule, "Heuristic_Schema_");
    }

    [TestMethod]
    public void Classify_DescriptionHasTimeout_ReturnsTransient()
    {
        var input = Build(description: "Operation failed due to TimeoutException after 30s");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Transient, result.Category);
        StringAssert.StartsWith(result.MatchedRule, "Heuristic_Transient_");
    }

    [TestMethod]
    public void Classify_DescriptionHas503_ReturnsDownstreamUnavailable()
    {
        var input = Build(description: "Got 503 Service Unavailable from downstream");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.DownstreamUnavailable, result.Category);
        StringAssert.StartsWith(result.MatchedRule, "Heuristic_Downstream_");
    }

    [TestMethod]
    public void Classify_HeuristicMatchIsCaseInsensitive()
    {
        // Per implementation: StringComparison.OrdinalIgnoreCase
        var input = Build(description: "got TIMEOUTEXCEPTION");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Transient, result.Category);
    }

    [TestMethod]
    public void Classify_SchemaMarkerWinsOverTransientMarkerInSameDescription()
    {
        // Schema markers are checked before transient markers in source order.
        var input = Build(description: "JsonException after TimeoutException retry");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.SchemaViolation, result.Category);
    }

    // ---- Catch-all ----

    [TestMethod]
    public void Classify_NoSignalsAtAll_ReturnsUnknown()
    {
        var input = Build();

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Unknown, result.Category);
        Assert.AreEqual("CatchAll_Unknown", result.MatchedRule);
        StringAssert.Contains(result.Reasoning, "no classifier rule matched");
    }

    [TestMethod]
    public void Classify_UnknownReasonAndUnknownDescription_ReturnsUnknown()
    {
        var input = Build(reason: "SomethingWeird", description: "weird description");

        var result = _target.Classify(input);

        Assert.AreEqual(FailureCategory.Unknown, result.Category);
    }

    // ---- Reasoning string content ----

    [TestMethod]
    public void Classify_ReasoningIncludesSourceAndDeliveryCount()
    {
        var input = Build(reason: "TTLExpiredException", deliveryCount: 7, sourceEntity: "my-queue");

        var result = _target.Classify(input);

        StringAssert.Contains(result.Reasoning, "Source='my-queue'");
        StringAssert.Contains(result.Reasoning, "DeliveryCount=7");
    }

    [TestMethod]
    public void Classify_TruncatesLongDescriptionInReasoning()
    {
        // Truncate at 120 chars + "..."
        var longDesc = "POISON_" + new string('x', 200);
        var input = Build(description: longDesc);

        var result = _target.Classify(input);

        // Reasoning should not contain the full 200-char tail
        Assert.IsFalse(result.Reasoning.Contains(new string('x', 200)),
            "Reasoning must truncate long descriptions");
        StringAssert.Contains(result.Reasoning, "...");
    }

    // ---- Null guard ----

    [TestMethod]
    public void Classify_NullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _target.Classify(null!));
    }
}
