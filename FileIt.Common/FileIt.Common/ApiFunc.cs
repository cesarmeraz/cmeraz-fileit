using System.Text.Json;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.Common.App;
using FileIt.Common.App.ApiAdd;
using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Common;

public class ApiFunc
{
    private readonly IApiAddCommand _command;
    private readonly CommonConfig _config;
    private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
    private readonly IApiLogRepo _apiLogRepo;
    private readonly ILogger<ApiFunc> _logger;

    public ApiFunc(
        IApiAddCommand command,
        ILogger<ApiFunc> logger,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        IApiLogRepo apiLogRepo,
        CommonConfig config
    )
    {
        _command = command;
        _logger = logger;
        _senderFactory = senderFactory;
        _apiLogRepo = apiLogRepo;
        _config = config;
    }

    [Function(nameof(ApiAdd))]
    public async Task ApiAdd([ServiceBusTrigger("api-add")] ServiceBusReceivedMessage message)
    {
        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", message.CorrelationId ?? string.Empty },
                    { "EventId", _config.AddEventId },
                }
            )
        )
        {
            ApiAddPayload? payload = null;
            string? bodystr = message.Body?.ToString();
            if (!string.IsNullOrWhiteSpace(bodystr))
            {
                payload = JsonSerializer.Deserialize<ApiAddPayload>(bodystr);
            }
            await _command.ApiAdd(
                new ApiRequest()
                {
                    Body = payload,
                    CorrelationId = message.CorrelationId,
                    MessageId = message.MessageId,
                    QueueName = _config.ApiAddQueueName,
                    ReplyTo = _config.ApiAddTopicName,
                    Subject = message.Subject,
                }
            );
        }
    }
}
