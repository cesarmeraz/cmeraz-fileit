using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Common.App.ApiAdd;

public interface IApiAddCommand
{
    Task ApiAdd(ApiRequest request);
}

public class ApiAddCommand : IApiAddCommand
{
    private readonly CommonConfig _config;
    private readonly IApiLogRepo _apiLogRepo;
    private readonly IBroadcastResponses _broadcaster;
    private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
    private readonly ILogger<ApiAddCommand> _logger;

    public ApiAddCommand(
        CommonConfig config,
        IApiLogRepo apiLogRepo,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        ILogger<ApiAddCommand> logger,
        IBroadcastResponses broadcaster
    )
    {
        _broadcaster = broadcaster;
        _config = config;
        _apiLogRepo = apiLogRepo;
        _senderFactory = senderFactory;
        _logger = logger;
    }

    public async Task ApiAdd(ApiRequest request)
    {
        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", request.CorrelationId ?? string.Empty },
                    { "EventId", _config.AddEventId },
                }
            )
        )
        {
            //simulate API action here
            var apiLogItem = await _apiLogRepo.AddAsync(
                request.CorrelationId ?? string.Empty,
                "Request body",
                "Response body",
                "Imaginary"
            );

            //Here we evaluate API response and return a result
            //var apiAddResult = new ApiAddResult() { };

            string body = JsonSerializer.Serialize(apiLogItem);

            _logger.LogDebug("The ApiAdd log item was created:\n{@ApiLogItem}", apiLogItem);

            var response = new ApiAddResponse()
            {
                NodeId = apiLogItem!.Id,
                CorrelationId = request.CorrelationId,
                TopicName = request.ReplyTo!,
            };

            await _broadcaster.Emit(response);
        }
    }
}
