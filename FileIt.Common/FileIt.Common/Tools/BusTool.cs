using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Common.Tools
{
    public interface IBusTool
    {
        /// <summary>
        /// Adds a Message to the Server Bus Queue
        /// </summary>
        /// <param name="queueOrTopicName">a string</param>
        /// <param name="message">a ServiceBusMessage</param>
        /// <returns></returns>
        Task SendMessageAsync(string queueOrTopicName, ServiceBusMessage message);
    }

    public class BusTool : IBusTool
    {
        private readonly ILogger<BusTool> _logger;
        private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;

        public BusTool(ILogger<BusTool> logger, IAzureClientFactory<ServiceBusSender> senderFactory)
        {
            _logger = logger;
            _senderFactory = senderFactory;
        }

        public async Task SendMessageAsync(string queueOrTopicName, ServiceBusMessage message)
        {
            if (string.IsNullOrEmpty(queueOrTopicName))
            {
                _logger.LogError("Queue name cannot be null or empty.");
                throw new ArgumentException(
                    "Queue name cannot be null or empty.",
                    nameof(queueOrTopicName)
                );
            }
            if (message == null)
            {
                _logger.LogError("ServiceBusMessage cannot be null.");
                throw new ArgumentNullException(
                    nameof(message),
                    "ServiceBusMessage cannot be null."
                );
            }
            ServiceBusSender sender = _senderFactory.CreateClient(queueOrTopicName);
            await sender.SendMessageAsync(message);
        }
    }
}
