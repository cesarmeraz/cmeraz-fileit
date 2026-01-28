using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

public class MiddlewareLogger : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        string functionName = context.FunctionDefinition.Name;
        // Code to execute before the function runs
        context
            .GetLogger<MiddlewareLogger>()
            .LogInformation("Start of function {Function} execution.", functionName);

        await next(context); // Calls the next middleware in the pipeline or the function itself

        // Code to execute after the function runs
        context
            .GetLogger<MiddlewareLogger>()
            .LogInformation("End of function {Function} execution.", functionName);
    }
}
