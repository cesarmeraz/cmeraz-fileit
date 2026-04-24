using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Logging;

public class MiddlewareLogger : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        string functionName = context.FunctionDefinition.Name;

        // Code to execute before the function runs
        context
            .GetLogger<MiddlewareLogger>()
            .LogInformation(
                InfrastructureEvents.FunctionStart,
                "Start function {Function}.",
                functionName
            );

        await next(context); // Calls the next middleware in the pipeline or the function itself

        // Code to execute after the function runs
        context
            .GetLogger<MiddlewareLogger>()
            .LogInformation(
                InfrastructureEvents.FunctionEnd,
                "End function {Function}.",
                functionName
            );
    }
}
