using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.App.Api;
using FileIt.App.Functions;
using FileIt.App.Models;
using FileIt.App.Providers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Simple;

public class SimpleSubscriber : BaseFunction
{
    private const int EventId = 2000;
    private const string WORKING_CONTAINER = "simple-working";
    private const string FINAL_CONTAINER = "simple-final";
    private readonly ISimpleRequestLogRepo _requestLogRepo;
    private readonly IBlobProvider _blobProvider;
    private readonly IBusProvider _busProvider;

    public SimpleSubscriber(
        ILogger<SimpleSubscriber> logger,
        IBlobProvider blobProvider,
        IBusProvider busProvider,
        ISimpleRequestLogRepo requestLogRepo
    )
        : base(logger, nameof(SimpleSubscriber))
    {
        _blobProvider = blobProvider;
        _busProvider = busProvider;
        _requestLogRepo = requestLogRepo;
    }

    /// <summary>
    /// A ServiceBusTrigger that processes the file ingested
    /// </summary>
    /// <param name="message">the ServiceBusReceivedMessage</param>
    /// <returns></returns>
    [Function(nameof(SimpleSubscriber))]
    public async Task Run(
        [ServiceBusTrigger("api-add-simple", "api-add-simple-sub")]
            ServiceBusReceivedMessage message
    )
    {
        string clientRequestId = message.CorrelationId ?? string.Empty;

        using (
            logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "ClientRequestId", clientRequestId ?? string.Empty },
                    { "EventId", EventId },
                    { "Module", MODULE_NAME },
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
            await _blobProvider.MoveBlobAsync(entry.BlobName, WORKING_CONTAINER, FINAL_CONTAINER);

            entry.ApiId = apiLog?.Id ?? 0;
            await _requestLogRepo.UpdateAsync(entry);
            logger.LogInformation($"Processed Simple Request Log: {entry}");
            LogFunctionEnd(nameof(SimpleSubscriber));
        }
    }
}
