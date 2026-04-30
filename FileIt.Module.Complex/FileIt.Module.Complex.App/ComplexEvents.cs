using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App;

/// <summary>
/// EventId catalog for the Complex module. Block 4000-4099 reserved per
/// scripts/new-fileit-module.ps1 convention (Services=1000, SimpleFlow=2000,
/// DataFlow=3000, Complex=4000).
/// </summary>
public class ComplexEvents
{
    // Document lifecycle (4000-4019)
    public static EventId DocumentCreated         = new(4000, nameof(DocumentCreated));
    public static EventId DocumentRetrieved       = new(4001, nameof(DocumentRetrieved));
    public static EventId DocumentNotFound        = new(4002, nameof(DocumentNotFound));
    public static EventId DocumentListed          = new(4003, nameof(DocumentListed));
    public static EventId DocumentSoftDeleted     = new(4004, nameof(DocumentSoftDeleted));
    public static EventId DocumentExportRequested = new(4005, nameof(DocumentExportRequested));

    // Validation / errors (4020-4039)
    public static EventId InvalidRequest          = new(4020, nameof(InvalidRequest));
    public static EventId NameTooLong             = new(4021, nameof(NameTooLong));
    public static EventId ContentTooLarge         = new(4022, nameof(ContentTooLarge));

    // Idempotency (4040-4049)
    public static EventId IdempotentReplay        = new(4040, nameof(IdempotentReplay));
    public static EventId IdempotencyKeyConflict  = new(4041, nameof(IdempotencyKeyConflict));
    public static EventId IdempotencyKeyRejected  = new(4042, nameof(IdempotencyKeyRejected));

    // Chaos / behavior (4060-4069)
    public static EventId LatencyInjected         = new(4060, nameof(LatencyInjected));
    public static EventId ChaosFailureInjected    = new(4061, nameof(ChaosFailureInjected));

    // Swagger / health (4080-4089)
    public static EventId SwaggerSpecServed       = new(4080, nameof(SwaggerSpecServed));
    public static EventId HealthChecked           = new(4081, nameof(HealthChecked));
}
