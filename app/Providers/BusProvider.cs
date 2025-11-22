using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Providers
{
    public interface IBusProvider
    {
        /// <summary>
        /// Adds a Message to the Server Bus Queue
        /// </summary>
        /// <param name="queueName">a string</param>
        /// <param name="message">a ServiceBusMessage</param>
        /// <returns></returns>
        Task SendMessageAsync(string queueName, ServiceBusMessage message);
    }

    public class BusProvider : IBusProvider
    {
        private readonly ILogger<BusProvider> _logger;
        private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;

        public BusProvider(
            ILogger<BusProvider> logger,
            IAzureClientFactory<ServiceBusSender> senderFactory
        )
        {
            _logger = logger;
            _senderFactory = senderFactory;
        }

        public async Task SendMessageAsync(string queueName, ServiceBusMessage message)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                _logger.LogError("Queue name cannot be null or empty.");
                throw new ArgumentException(
                    "Queue name cannot be null or empty.",
                    nameof(queueName)
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
            ServiceBusSender sender = _senderFactory.CreateClient(queueName);
            await sender.SendMessageAsync(message);
        }
    }
}
