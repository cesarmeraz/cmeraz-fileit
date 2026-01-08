using FileIt.Common.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FileIt.App;

public class HealthCheck : BaseFunction
{
    public HealthCheck(ILogger<HealthCheck> logger)
        : base(logger, nameof(HealthCheck)) { }

    [Function("HealthCheck")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req
    )
    {
        LogFunctionStart(nameof(Run));
        logger.LogInformation("C# HTTP trigger function processed a request.");
        LogFunctionEnd(nameof(Run));
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
