using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Common.App;
using FileIt.Common.App.ApiAdd;
using FileIt.Domain.Entities.Api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Common;

public class ApiFunc
{
    private readonly IApiAddCommand _command;
    private readonly CommonConfig _config;
    private readonly ILogger<ApiFunc> _logger;

    public ApiFunc(IApiAddCommand command, ILogger<ApiFunc> logger, CommonConfig config)
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
            _logger.LogInformation(CommonEvents.AddEvent.Id, "ApiAdd started");
            ApiAddPayload? payload = null;
            string? bodystr = message.Body?.ToString();
            if (!string.IsNullOrWhiteSpace(bodystr))
            {
                payload = JsonSerializer.Deserialize<ApiAddPayload>(bodystr);
                _logger.LogDebug(
                    CommonEvents.GetPayload.Id,
                    "ApiAdd payload:\n{@ApiPayload}",
                    payload
                );
            }
            var request = new ApiRequest(message.MessageId)
            {
                Body = payload,
                CorrelationId = message.CorrelationId,
                QueueName = _config.ApiAddQueueName,
                ReplyTo = _config.ApiAddTopicName,
                Subject = message.Subject,
            };
            _logger.LogDebug(CommonEvents.ExecApiAdd.Id, "ApiAdd request:\n{@ApiRequest}", request);
            await _command.ApiAdd(request);
        }
    }
}
