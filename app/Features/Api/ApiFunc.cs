using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.App.Api;
using FileIt.App.Features.Api;
using FileIt.App.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Functions.Api
{
    public class ApiFunc : BaseFunction
    {
        private readonly ApiConfig _config;
        private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
        private readonly IApiLogRepo _apiLogRepo;

        public ApiFunc(
            ILogger<ApiFunc> logger,
            IAzureClientFactory<ServiceBusSender> senderFactory,
            IApiLogRepo apiLogRepo,
            ApiConfig config
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
                        { "ClientRequestId", message.MessageId ?? string.Empty },
                        { "EventId", _config.AddEventId },
                        { "Feature", message.Subject },
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
