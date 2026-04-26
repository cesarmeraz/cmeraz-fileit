using Azure.Messaging.ServiceBus;
using FileIt.Module.Services.App;
using FileIt.Module.Services.App.ApiAdd;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Services.Host;

public class ApiFunc
{
    private readonly IApiAddCommand _command;
    private readonly ServicesConfig _config;
    private readonly ILogger<ApiFunc> _logger;

    public ApiFunc(IApiAddCommand command, ILogger<ApiFunc> logger, ServicesConfig config)
    {
        _command = command;
        _logger = logger;
        _config = config;
    }

    [Function(nameof(ApiAdd))]
    public async Task ApiAdd(
        [ServiceBusTrigger("api-add", Connection = "FileItServiceBus")]
            ServiceBusReceivedMessage message
    )
    {
        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", message.CorrelationId ?? string.Empty },
                }
            )
        )
        {
            _logger.LogInformation(ServicesEvents.AddEvent, "ApiAdd started");
            string? bodystr = message.Body?.ToString();
            await _command.ApiAdd(
                message.MessageId,
                _config.ApiAddQueueName,
                message.Subject,
                _config.ApiAddTopicName,
                message.CorrelationId,
                bodystr
            );
        }
    }
}
