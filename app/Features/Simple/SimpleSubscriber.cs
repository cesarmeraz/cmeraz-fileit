using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.App.Api;
using FileIt.App.Functions;
using FileIt.App.Providers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Features.Simple;

public class SimpleSubscriber : BaseFunction
{
    private readonly ISimpleRequestLogRepo _requestLogRepo;
    private readonly IBlobTool _blobTool;
    private readonly IBusTool _busTool;
    private readonly SimpleConfig _config;

    public SimpleSubscriber(
        ILogger<SimpleSubscriber> logger,
        IBlobTool blobTool,
        IBusTool busTool,
        ISimpleRequestLogRepo requestLogRepo,
        SimpleConfig config
    )
        : base(logger, nameof(SimpleSubscriber))
    {
        _blobTool = blobTool;
        _busTool = busTool;
        _requestLogRepo = requestLogRepo;
        _config = config;
    }

    /// <summary>
    /// A ServiceBusTrigger that processes the file ingested
    /// </summary>
    /// <param name="message">the ServiceBusReceivedMessage</param>
    /// <returns></returns>
    [Function(nameof(SimpleSubscriber))]
    public async Task Run(
        [ServiceBusTrigger("api-add-topic", "api-add-simple-sub")] ServiceBusReceivedMessage message
    )
    {
        string clientRequestId = message.CorrelationId ?? string.Empty;

        using (
            logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "ClientRequestId", clientRequestId ?? string.Empty },
                    { "EventId", _config.SimpleSubscriberEventId },
                    { "Feature", _config.FeatureName },
                }
            )
        )
        {
            LogFunctionStart(nameof(SimpleSubscriber));
            logger.LogInformation("Processing {@message}", message);
            string messageBody = message.Body.ToString();
            logger.LogInformation("Deserializing {messageBody}", messageBody);
            ApiLog? apiLog;
            try
            {
                apiLog = JsonSerializer.Deserialize<ApiLog>(messageBody);
                logger.LogInformation("Deserialized ApiLog: {@ApiLog}", apiLog);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize message body to ApiLog");
                throw;
            }
            SimpleRequestLog? entry = await _requestLogRepo.GetByClientRequestIdAsync(
                clientRequestId
            );
            if (entry == null)
            {
                logger.LogError(
                    "No SimpleRequestLog found for ClientRequestId: {ClientRequestId}",
                    clientRequestId
                );
                throw new Exception("SimpleRequestLog entry not found");
            }
            //Process the file then
            await _blobTool.MoveBlobAsync(
                entry.BlobName,
                _config.WorkingContainer,
                _config.FinalContainer
            );

            entry.ApiId = apiLog?.Id ?? 0;
            await _requestLogRepo.UpdateAsync(entry);
            logger.LogInformation("Processed Simple Request Log: {@entry}", entry);
            LogFunctionEnd(nameof(SimpleSubscriber));
        }
    }
}
