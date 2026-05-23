using FileIt.Module.Complex.App;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.Host.Endpoints;

/// <summary>
/// Liveness endpoint. Exempt from chaos so dashboards stay green even when
/// the synthetic-failure dial is cranked up.
/// </summary>
public class Health
{
    private readonly ILogger<Health> _logger;

    public Health(ILogger<Health> logger)
    {
        _logger = logger;
    }

    [Function("Health")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
            HttpRequest req)
    {
        _logger.LogInformation(ComplexEvents.HealthChecked, "Health probe");
        return new OkObjectResult(new
        {
            status = "ok",
            module = "complex",
            utc = DateTime.UtcNow,
        });
    }
}
