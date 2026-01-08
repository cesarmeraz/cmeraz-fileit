using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.Common.Functions;
using FileIt.Common.Tools;
using FileIt.SimpleProvider;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FileIt.SimpleProvider;

public class SimpleWatcher : BaseFunction
{
    private readonly IBlobTool _blobTool;
    private readonly IBusTool _busTool;
    private readonly SimpleConfig _config;
    private readonly ISimpleRequestLogRepo _requestLogRepo;

    public SimpleWatcher(
        ILogger<SimpleWatcher> logger,
        IBlobTool blobTool,
        IBusTool busTool,
        ISimpleRequestLogRepo requestLogRepo,
        SimpleConfig config
    )
        : base(logger, config.Feature)
    {
        _blobTool = blobTool;
        _busTool = busTool;
        _config = config;
        _requestLogRepo = requestLogRepo;
    }

    /// <summary>
    /// a BlobTrigger that receives the BlobClient and its name
    /// </summary>
    /// <param name="blobClient">the BlobClient</param>
    /// <param name="blobName">the file name</param>
    /// <returns></returns>
    [Function(nameof(SimpleWatcher))]
    public async Task Run(
        [BlobTrigger("simple-source/{blobName}")] BlobClient blobClient,
        string blobName
    )
    {
        blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

        // use the blobClient to get the x-ms-client-request-id property from the original request header
        string clientRequestId = await GetCorrelationIdFromHeaderAsync(blobClient);

        using (
            logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", clientRequestId ?? string.Empty },
                    { "EventId", _config.SimpleIntakeEventId },
                }
            )
        )
        {
            LogFunctionStart(nameof(SimpleWatcher));
            logger.LogInformation(
                "Received blob trigger for blob: {BlobName} with ClientRequestId: {CorrelationId}",
                blobName,
                clientRequestId
            );

            await _requestLogRepo.AddAsync(blobName, clientRequestId ?? string.Empty);

            await _blobTool.MoveBlobAsync(
                blobName,
                _config.SourceContainer,
                _config.WorkingContainer
            );
            // Get record from Blob storage to parse metadata and properties
            // _busProvider.
            var messageObject = new SimpleMessage { BlobName = blobName };
            ServiceBusMessage message = new ServiceBusMessage(
                JsonSerializer.Serialize(messageObject)
            );
            message.MessageId = clientRequestId;
            message.ReplyTo = _config.ApiAddTopicName;
            message.Subject = _config.Feature;
            message.ContentType = "application/json";
            message.ApplicationProperties.Add("CLIENT_REQUEST_ID", clientRequestId);
            message.ApplicationProperties.Add("BLOB_NAME", blobName);
            await _busTool.SendMessageAsync(_config.ApiAddQueueName, message);
            LogFunctionEnd(nameof(SimpleWatcher));
        }
    }
}
