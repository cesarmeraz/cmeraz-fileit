using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Tools;

public class PublishTool : IBroadcastResponses
{
    private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
    private readonly ILogger<PublishTool> _logger;

    public PublishTool(
        IAzureClientFactory<ServiceBusSender> senderFactory,
        ILogger<PublishTool> logger
    )
    {
        _senderFactory = senderFactory;
        _logger = logger;
    }

    public async Task EmitAsync(ApiAddResponse response)
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));
        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", response.CorrelationId ?? string.Empty },
                }
            )
        )
        {
            if (string.IsNullOrWhiteSpace(response.TopicName))
            {
                ;
                _logger.LogError(
                    InfrastructureEvents.PublishToolEmitInvalid,
                    "TopicName is missing."
                );
                throw new ArgumentException("TopicName is missing.");
            }
            _logger.LogInformation(
                InfrastructureEvents.PublishToolEmitStart,
                "Emitting response to {TopicName}",
                response.TopicName
            );
            var body = JsonSerializer.Serialize(response);
            var returnMessage = new ServiceBusMessage(body)
            {
                CorrelationId = response.CorrelationId,
                Subject = response.Subject,
                ContentType = "application/json",
            };

            ServiceBusSender sender;
            try
            {
                sender = _senderFactory.CreateClient(response.TopicName);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(
                    InfrastructureEvents.PublishToolEmitError,
                    ex,
                    "Unable to create client for topic {TopicName}",
                    response.TopicName
                );
                throw;
            }
            await sender.SendMessageAsync(returnMessage);
            _logger.LogInformation(
                InfrastructureEvents.PublishToolEmitEnd,
                "Returning response from Api"
            );
        }
    }
}
