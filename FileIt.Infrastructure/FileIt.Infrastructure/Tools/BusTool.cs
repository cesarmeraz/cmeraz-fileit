using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
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
            if (string.IsNullOrEmpty(request.QueueName))
            {
                _logger.LogError(
                    InfrastructureEvents.BusToolSendMessageValidationError.Id,
                    "Queue name cannot be null or empty."
                );
                throw new ArgumentException(
                    "Queue name cannot be null or empty.",
                    nameof(request.QueueName)
                );
            }
            _logger.LogInformation(
                InfrastructureEvents.BusToolSendMessageStart.Id,
                "Received message {MessageId} for {QueueName} queue",
                request.MessageId,
                request.QueueName
            );
            string? body = (request.Body == null) ? null : JsonSerializer.Serialize(request.Body);
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
                InfrastructureEvents.BusToolSendMessageEnqueued.Id,
                "Enqueued message {MessageId} to {QueueName} queue",
                request.MessageId,
                request.QueueName
            );
        }
    }
}
