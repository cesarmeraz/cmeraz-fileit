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

        public async Task SendMessageAsync(ApiRequest request, CancellationToken cancellationToken = default)
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

            ServiceBusMessage message;
            if (request.Body == null)
            {
                message = new ServiceBusMessage();
            }
            else
            {
                var body = JsonSerializer.Serialize(request.Body);
                message = new ServiceBusMessage(body);
            }

            message.ContentType = "application/json";
            message.CorrelationId = request.CorrelationId;
            message.MessageId = request.MessageId;
            message.ReplyTo = request.ReplyTo;
            message.Subject = request.Subject;

            // Stamp the publish-time UTC clock on every outgoing message. Service Bus
            // does not preserve the original enqueue timestamp through dead-lettering,
            // so this property is the only reliable source of truth for the failure-age
            // delta computed by the dead-letter pipeline. ISO 8601 round-trippable.
            // See FileIt.Infrastructure.FileItMessageProperties for the contract.
            message.ApplicationProperties[FileItMessageProperties.EnqueuedTimeUtc] =
                DateTime.UtcNow.ToString("O");

            cancellationToken.ThrowIfCancellationRequested();

            ServiceBusSender sender = _senderFactory.CreateClient(request.QueueName);
            await sender.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                InfrastructureEvents.BusToolSendMessageEnqueued,
                "Enqueued message {MessageId} to {QueueName} queue",
                request.MessageId,
                request.QueueName
            );
        }
    }
}
