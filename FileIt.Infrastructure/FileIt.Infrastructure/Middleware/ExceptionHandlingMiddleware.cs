using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Middleware
{
    public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    InfrastructureEvents.UnhandledException,
                    ex,
                    "Unhandled exception while processing request");

                var httpContext = context.GetHttpContext();

                if (httpContext is null)
                {
                    // Non-HTTP trigger (Service Bus, Timer, Blob, etc).
                    // We MUST rethrow so the Functions runtime treats this as a
                    // failed invocation. For Service Bus triggers, that triggers
                    // delivery-count increment and eventual dead-lettering once
                    // MaxDeliveryCount is exceeded - which is the entire foundation
                    // of the dead-letter strategy in docs/dead-letter-strategy.md.
                    // Swallowing here would silently complete failed messages and
                    // make the DLQ pipeline unreachable.
                    throw;
                }

                if (!httpContext.Response.HasStarted)
                {
                    // HTTP trigger and we still own the response. Format a generic
                    // 500 for the client; full exception detail is in the log above.
                    httpContext.Response.Clear();
                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    httpContext.Response.ContentType = "application/json";

                    var payload = JsonSerializer.Serialize(new
                    {
                        error = "An internal server error occurred.",
                        timestamp = DateTime.UtcNow
                    });
                    await httpContext.Response.WriteAsync(payload);
                    return;
                }

                // HTTP trigger but the response has already started streaming.
                // We cannot retroactively change status; rethrow so the runtime
                // logs the failure and aborts the response stream cleanly.
                _logger.LogWarning(
                    "Response has already started; rethrowing exception so runtime "
                    + "can mark the invocation as failed.");
                throw;
            }
        }
    }
}
