using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.App.Functions;
using FileIt.App.Providers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FileIt.App.Features.Simple;

public class SimpleIntake : BaseFunction
{
    private readonly IBlobTool _blobProvider;
    private readonly IBusTool _busProvider;
    private readonly SimpleConfig _config;
    private readonly ISimpleRequestLogRepo _requestLogRepo;

    public SimpleIntake(
        ILogger<SimpleIntake> logger,
        IBlobTool blobProvider,
        IBusTool busProvider,
        ISimpleRequestLogRepo requestLogRepo,
        SimpleConfig config
    )
        : base(logger, config.FeatureName)
    {
        _blobProvider = blobProvider;
        _busProvider = busProvider;
        _config = config;
        _requestLogRepo = requestLogRepo;
    }

    /// <summary>
    /// a BlobTrigger that receives the BlobClient and its name
    /// </summary>
    /// <param name="blobClient">the BlobClient</param>
    /// <param name="blobName">the file name</param>
    /// <returns></returns>
    [Function(nameof(SimpleIntake))]
    public async Task Run(
        [BlobTrigger("simple-source/{blobName}")] BlobClient blobClient,
        string blobName
    )
    {
        blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

        // use the blobClient to get the x-ms-client-request-id property from the original request header
        string clientRequestId = await GetClientRequestIdFromHeaderAsync(blobClient);

        using (
            logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "ClientRequestId", clientRequestId ?? string.Empty },
                    { "EventId", _config.SimpleIntakeEventId },
                    { "Feature", _config.FeatureName },
                }
            )
        )
        {
            LogFunctionStart(nameof(SimpleIntake));
            logger.LogInformation(
                "Received blob trigger for blob: {BlobName} with ClientRequestId: {ClientRequestId}",
                blobName,
                clientRequestId
            );

            await _requestLogRepo.AddAsync(blobName, clientRequestId ?? string.Empty);

            await _blobProvider.MoveBlobAsync(
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
            message.Subject = _config.FeatureName;
            message.ContentType = "application/json";
            message.ApplicationProperties.Add("CLIENT_REQUEST_ID", clientRequestId);
            message.ApplicationProperties.Add("BLOB_NAME", blobName);
            message.ApplicationProperties.Add("SOURCE", _config.WorkingContainer);
            message.ApplicationProperties.Add("DESTINATION", _config.FinalContainer);
            await _busProvider.SendMessageAsync(_config.ApiAddQueueName, message);
            LogFunctionEnd(nameof(SimpleIntake));
        }
    }
}
