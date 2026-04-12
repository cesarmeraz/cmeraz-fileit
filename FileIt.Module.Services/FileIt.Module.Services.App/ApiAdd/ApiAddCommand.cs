using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Services.App.ApiAdd;

public interface IApiAddCommand
{
    Task ApiAdd(ApiRequest request);
}

public class ApiAddCommand : IApiAddCommand
{
    private readonly IApiLogRepo _apiLogRepo;
    private readonly IBroadcastResponses _broadcaster;
    private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
    private readonly ILogger<ApiAddCommand> _logger;

    public ApiAddCommand(
        IApiLogRepo apiLogRepo,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        ILogger<ApiAddCommand> logger,
        IBroadcastResponses broadcaster
    )
    {
        _broadcaster = broadcaster;
        _apiLogRepo = apiLogRepo;
        _senderFactory = senderFactory;
        _logger = logger;
    }

    public async Task ApiAdd(ApiRequest request)
    {
        _logger.LogInformation(ServicesEvents.LogApiAddRequest.Id, "Simulating API action");
        var apiLogItem = await _apiLogRepo.AddAsync(
            request.CorrelationId ?? string.Empty,
            "Request body",
            "Response body",
            "Imaginary"
        );

        //Here we evaluate API response and return a result
        var response = new ApiAddResponse()
        {
            NodeId = apiLogItem!.Id,
            CorrelationId = request.CorrelationId,
            TopicName = request.ReplyTo!,
        };

        _logger.LogDebug(
            ServicesEvents.ApiAddResponsePublished.Id,
            "Response published:\n{@ApiAddResponse}",
            response
        );
        await _broadcaster.EmitAsync(response);
    }
}
