using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.Common.Functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.App
{
    public class ApiFunc : BaseFunction
    {
        private readonly AppConfig _config;
        private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
        private readonly IApiLogRepo _apiLogRepo;

        public ApiFunc(
            ILogger<ApiFunc> logger,
            IAzureClientFactory<ServiceBusSender> senderFactory,
            IApiLogRepo apiLogRepo,
            AppConfig config
        )
            : base(logger, nameof(ApiFunc))
        {
            _senderFactory = senderFactory;
            _apiLogRepo = apiLogRepo;
            _config = config;
        }

        [Function(nameof(ApiAdd))]
        public async Task ApiAdd([ServiceBusTrigger("api-add")] ServiceBusReceivedMessage message)
        {
            string clientRequestId = message.MessageId;

            using (
                logger!.BeginScope(
                    new Dictionary<string, object>()
                    {
                        { "CorrelationId", message.MessageId ?? string.Empty },
                        { "EventId", _config.AddEventId },
                    }
                )
            )
            {
                LogFunctionStart(nameof(ApiAdd));
                var apiLogItem = await _apiLogRepo.AddAsync(
                    clientRequestId,
                    "Request body",
                    "Response body",
                    "Imaginary"
                );
                string body = JsonSerializer.Serialize(apiLogItem);

                logger.LogDebug("The ApiAdd log item was created:\n{ApiLogItem}", body);

                var returnMessage = new ServiceBusMessage(body)
                {
                    CorrelationId = clientRequestId,
                    Subject = message.Subject,
                    ContentType = "application/json",
                };

                ServiceBusSender sender = _senderFactory.CreateClient(_config.ApiAddTopicName);
                await sender.SendMessageAsync(returnMessage);
                LogFunctionEnd(nameof(ApiAdd));
            }
        }
    }
}
