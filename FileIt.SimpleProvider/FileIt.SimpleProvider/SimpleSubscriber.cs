using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Simple;
using FileIt.SimpleProvider.App;
using FileIt.SimpleProvider.App.WaitOnApiUpload;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.SimpleProvider;

public class SimpleSubscriber
{
    private readonly SimpleConfig _config;
    private readonly ILogger<SimpleSubscriber> _logger;
    private readonly IBasicApiAddHandler _responseHandler;

    public SimpleSubscriber(
        ILogger<SimpleSubscriber> logger,
        SimpleConfig config,
        IBasicApiAddHandler responseHandler
    )
    {
        _config = config;
        _logger = logger;
        _responseHandler = responseHandler;
    }

    /// <summary>
    /// A ServiceBusTrigger that processes the file ingested
    /// </summary>
    /// <param name="message">the ServiceBusReceivedMessage</param>
    /// <returns></returns>
    [Function(nameof(SimpleSubscriber))]
    public async Task Run(
        [ServiceBusTrigger("api-add-topic", "api-add-simple-sub")] ServiceBusReceivedMessage message
    )
    {
        string clientRequestId = message.CorrelationId ?? string.Empty;

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", clientRequestId ?? string.Empty },
                    { "EventId", SimpleEvents.SimpleSubscriber },
                }
            )
        )
        {
            //LogFunctionStart(nameof(SimpleSubscriber));
            _logger.LogInformation("Processing {@message}", message);

            var response = JsonSerializer.Deserialize<ApiAddResponse>(message.Body.ToString());
            if (response == null)
            {
                _logger.LogWarning("Failed to deserialize ApiAddResponse!");
                throw new ApplicationException("Failed to deserialize ApiAddResponse!");
            }
            await _responseHandler.RunAsync(response);
            //LogFunctionEnd(nameof(SimpleSubscriber));
        }
    }
}
