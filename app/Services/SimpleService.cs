using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using FileIt.App.Models;
using FileIt.App.Providers;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Services
{
    public interface ISimpleService
    {
        Task ProcessAsync(ServiceBusReceivedMessage message);
        Task QueueAsync(Stream stream, string name);
        Task<bool> ValidateBlobAsync(Stream stream, string name);
    }

    public class SimpleService : ISimpleService
    {
        private const string QUEUE_NAME = "simple";
        private const string SOURCE_CONTAINER = "simple-source";
        private const string WORKING_CONTAINER = "simple-working";
        private const string FINAL_CONTAINER = "simple-final";

        private readonly ILogger<SimpleService> _logger;
        private readonly IBlobProvider _blobProvider;
        private readonly IBusProvider _busProvider;

        public SimpleService(
            ILogger<SimpleService> logger,
            IBlobProvider blobProvider,
            IBusProvider busProvider
        )
        {
            _blobProvider = blobProvider;
            _busProvider = busProvider;
            _logger = logger;
        }

        public async Task ProcessAsync(ServiceBusReceivedMessage message)
        {
            var name = message.ApplicationProperties["BLOB_NAME"].ToString();
            //Process the file then
            await _blobProvider.MoveBlobAsync(name, WORKING_CONTAINER, FINAL_CONTAINER);
        }

        public async Task QueueAsync(Stream stream, string name)
        {
            _logger.LogInformation($"blob name: {name}");
            await _blobProvider.MoveBlobAsync(name, SOURCE_CONTAINER, WORKING_CONTAINER);
            // Get record from Blob storage to parse metadata and properties
            // _busProvider.
            var messageObject = new SimpleMessage { BlobName = name };
            ServiceBusMessage message = new ServiceBusMessage(
                JsonSerializer.Serialize(messageObject)
            );
            message.ApplicationProperties.Add("BLOB_NAME", name);
            message.ApplicationProperties.Add("SOURCE", WORKING_CONTAINER);
            message.ApplicationProperties.Add("DESTINATION", FINAL_CONTAINER);
            await _busProvider.SendMessageAsync(QUEUE_NAME, message);
        }

        public async Task<bool> ValidateBlobAsync(Stream stream, string name)
        {
            // Placeholder for blob validation logic
            return true;
        }
    }
}
