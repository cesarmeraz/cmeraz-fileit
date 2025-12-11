using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.App.Functions;
using FileIt.App.Models;
using FileIt.App.Providers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace FileIt.App.Simple;

public class SimpleIntake : BaseFunction
{
    private const int EventId = 2000;
    private const string API_QUEUE_NAME = "api-add";
    private const string API_TOPIC_NAME = "api-add-simple";
    private const string QUEUE_NAME = "simple";
    private const string SOURCE_CONTAINER = "simple-source";
    private const string WORKING_CONTAINER = "simple-working";
    private const string FINAL_CONTAINER = "simple-final";
    private readonly IBlobProvider _blobProvider;
    private readonly IBusProvider _busProvider;
    private readonly ISimpleRequestLogRepo _requestLogRepo;

    public SimpleIntake(
        ILogger<SimpleIntake> logger,
        IBlobProvider blobProvider,
        IBusProvider busProvider,
        ISimpleRequestLogRepo requestLogRepo
    )
        : base(logger, nameof(SimpleIntake))
    {
        _blobProvider = blobProvider;
        _busProvider = busProvider;
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
                    { "EventId", EventId },
                    { "Module", MODULE_NAME },
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

            await _blobProvider.MoveBlobAsync(blobName, SOURCE_CONTAINER, WORKING_CONTAINER);
            // Get record from Blob storage to parse metadata and properties
            // _busProvider.
            var messageObject = new SimpleMessage { BlobName = blobName };
            ServiceBusMessage message = new ServiceBusMessage(
                JsonSerializer.Serialize(messageObject)
            );
            message.MessageId = clientRequestId;
            message.ReplyTo = API_TOPIC_NAME;
            message.Subject = MODULE_NAME;
            message.ContentType = "application/json";
            message.ApplicationProperties.Add("CLIENT_REQUEST_ID", clientRequestId);
            message.ApplicationProperties.Add("BLOB_NAME", blobName);
            message.ApplicationProperties.Add("SOURCE", WORKING_CONTAINER);
            message.ApplicationProperties.Add("DESTINATION", FINAL_CONTAINER);
            await _busProvider.SendMessageAsync(API_QUEUE_NAME, message);
            LogFunctionEnd(nameof(SimpleIntake));
        }
    }
}
