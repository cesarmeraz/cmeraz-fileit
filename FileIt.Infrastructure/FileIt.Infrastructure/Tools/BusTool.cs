using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Tools
{
    public class BusTool : ITalkToApi
    {
        private readonly ILogger<BusTool> _logger;
        private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;

        public BusTool(ILogger<BusTool> logger, IAzureClientFactory<ServiceBusSender> senderFactory)
        {
            _logger = logger;
            _senderFactory = senderFactory;
        }

        public async Task SendMessageAsync(ApiRequest request)
        {
            using (
                _logger!.BeginScope(
                    new Dictionary<string, object>()
                    {
                        { "CorrelationId", request.CorrelationId ?? string.Empty },
                    }
                )
            )
            {
                if (string.IsNullOrEmpty(request.QueueName))
                {
                    _logger.LogError(
                        InfrastructureEvents.BusToolSendMessageValidationError,
                        "Queue name cannot be null or empty."
                    );
                    throw new ArgumentException(
                        "Queue name cannot be null or empty.",
                        nameof(request.QueueName)
                    );
                }
                _logger.LogInformation(
                    InfrastructureEvents.BusToolSendMessageStart,
                    "Received message {MessageId} for {QueueName} queue",
                    request.MessageId,
                    request.QueueName
                );
                string? body =
                    (request.Body == null) ? null : JsonSerializer.Serialize(request.Body);
                ServiceBusMessage message =
                    request.Body == null ? new ServiceBusMessage() : new ServiceBusMessage(body);
                message.ContentType = "application/json";
                message.CorrelationId = request.CorrelationId;
                message.MessageId = request.MessageId;
                message.ReplyTo = request.ReplyTo;
                message.Subject = request.Subject;

                ServiceBusSender sender = _senderFactory.CreateClient(request.QueueName);
                await sender.SendMessageAsync(message);
                _logger.LogInformation(
                    InfrastructureEvents.BusToolSendMessageEnqueued,
                    "Enqueued message {MessageId} to {QueueName} queue",
                    request.MessageId,
                    request.QueueName
                );
            }
        }
    }
}
