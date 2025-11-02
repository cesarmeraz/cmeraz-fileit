using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Providers
{
    public interface IBusProvider
    {
        // Define methods for blob operations
        Task SendMessageAsync(string queueName, ServiceBusMessage message);
    }

    public class BusProvider : IBusProvider
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<BusProvider> _logger;

        public BusProvider(ILogger<BusProvider> logger, ServiceBusClient serviceBusClient)
        {
            // Initialize any required resources
            _logger = logger;
            _serviceBusClient = serviceBusClient;
        }

        public async Task SendMessageAsync(string queueName, ServiceBusMessage message)
        {
            ServiceBusSender sender = _serviceBusClient.CreateSender(queueName);
            await sender.SendMessageAsync(message);
            await sender.DisposeAsync(); // Dispose the sender when no longer needed
        }

        // Implement blob operations here
    }
}
