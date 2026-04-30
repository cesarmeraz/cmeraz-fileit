namespace FileIt.Module.Complex.App;

/// <summary>
/// Strongly-typed feature config for the Complex module. Bound from the
/// Feature configuration section in appsettings.json.
/// </summary>
public class ComplexConfig
{
    /// <summary>
    /// Base URL the module advertises in Location headers. Set to the
    /// public-facing function-app URL in production. Defaults to localhost
    /// for dev.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:7064";

    /// <summary>
    /// Page size cap for list endpoints. Requests asking for more get
    /// truncated to this value.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    public LatencyOptions Latency { get; set; } = new();
    public ChaosOptions Chaos { get; set; } = new();
    public IdempotencyOptions Idempotency { get; set; } = new();
}

public class LatencyOptions
{
    /// <summary>
    /// Whether to inject artificial delay on responses.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public int MinMs { get; set; } = 50;
    public int MaxMs { get; set; } = 300;
}

public class ChaosOptions
{
    /// <summary>
    /// Whether to inject artificial failures.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Probability (0.0 - 1.0) that a request returns 503 with Retry-After.
    /// 0.05 means roughly 1 in 20 requests will be rejected, exercising the
    /// caller's retry / DLQ pipeline.
    /// </summary>
    public double Failure503Rate { get; set; } = 0.05;

    /// <summary>
    /// Retry-After value (seconds) sent with synthetic 503 responses.
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 2;

    /// <summary>
    /// Endpoints exempt from chaos. Keep health and swagger out so demos
    /// don't crash.
    /// </summary>
    public string[] ExemptPaths { get; set; } = new[] { "/api/health", "/api/docs" };
}

public class IdempotencyOptions
{
    /// <summary>
    /// Whether to honour the Idempotency-Key request header on POST.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Header name. Standard is "Idempotency-Key" per the IETF draft.
    /// </summary>
    public string HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>
    /// Reject keys longer than this. Standard recommends &lt;= 255.
    /// </summary>
    public int MaxKeyLength { get; set; } = 128;
}
