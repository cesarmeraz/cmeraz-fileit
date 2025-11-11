using Azure.Messaging.ServiceBus;
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
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<BusProvider> _logger;

        public BusProvider(ILogger<BusProvider> logger, ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _serviceBusClient = serviceBusClient;
        }

        public async Task SendMessageAsync(string queueName, ServiceBusMessage message)
        {
            ServiceBusSender sender = _serviceBusClient.CreateSender(queueName);
            await sender.SendMessageAsync(message);
        }
    }
}
