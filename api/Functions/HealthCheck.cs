using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Api.Functions;

public class HealthCheck : BaseFunction
{
    private readonly ILogger<HealthCheck> _logger;

    public HealthCheck(ILogger<HealthCheck> logger)
        : base(logger)
    {
        _logger = logger;
    }

    [Function("HealthCheck")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req
    )
    {
        LogFunctionStart(nameof(Run));
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        LogFunctionEnd(nameof(Run));
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
