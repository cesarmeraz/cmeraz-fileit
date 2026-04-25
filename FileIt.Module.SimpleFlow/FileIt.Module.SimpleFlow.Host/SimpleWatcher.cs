using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using FileIt.Infrastructure.Extensions;
using FileIt.Module.SimpleFlow.App;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.SimpleFlow;

public class SimpleWatcher
{
    private readonly SimpleConfig _config;
    private readonly ILogger<SimpleWatcher> _logger;
    private readonly IWatchInbound _watcher;

    public SimpleWatcher(ILogger<SimpleWatcher> logger, SimpleConfig config, IWatchInbound watcher)
    {
        _config = config;
        _logger = logger;
        _watcher = watcher;
    }

#if DEBUG
    /// <summary>
    /// a BlobTrigger that receives the BlobClient and its name
    /// </summary>
    /// <param name="blobClient">the BlobClient</param>
    /// <param name="blobName">the file name</param>
    /// <returns></returns>
    [Function("SimpleWatcherLocal")]
    public async Task RunLocal(
        [BlobTrigger("simple-source/{blobName}")] BlobClient blobClient,
        string blobName
    )
    {
        blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

        // use the blobClient to get the x-ms-client-request-id property from the original request header
        string clientRequestId = await blobClient.GetCorrelationId();

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>() { { "CorrelationId", clientRequestId } }
            )
        )
        {
            _logger.LogInformation(
                SimpleEvents.SimpleWatcher,
                "Received blob trigger for blob: {BlobName}",
                blobName
            );

            await _watcher.RunAsync(blobName, clientRequestId);
        }
    }
#endif

    [Function(nameof(SimpleWatcher))]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        // use the blobClient to get the x-ms-client-request-id property from the original request header
        string clientRequestId = eventGridEvent.Id;

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>() { { "CorrelationId", clientRequestId } }
            )
        )
        {
            var blobName = (eventGridEvent.Subject ?? string.Empty).Split('/').Last();
            _logger.LogInformation(
                SimpleEvents.SimpleWatcher,
                "Received blob trigger for blob: {BlobName}",
                blobName
            );

            await _watcher.RunAsync(blobName, clientRequestId);
        }
    }
}
