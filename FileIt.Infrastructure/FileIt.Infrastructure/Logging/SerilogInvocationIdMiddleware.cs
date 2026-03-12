using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Serilog.Context;

namespace FileIt.Infrastructure.Logging;

public class SerilogInvocationIdMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Capture the unique InvocationId for this execution
        using (LogContext.PushProperty("InvocationId", context.InvocationId))
        {
            await next(context);
        }
    }
}
