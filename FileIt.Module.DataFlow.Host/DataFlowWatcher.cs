// This is the Azure Function that watches for new GL Account CSV files in blob storage.
// In local dev (DEBUG) it uses a direct blob trigger.
// In production it uses an Event Grid trigger — same pattern as SimpleWatcher.
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using FileIt.Infrastructure.Extensions;
using FileIt.Module.DataFlow.App;
using FileIt.Module.DataFlow.App.WatchInbound;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.Host;

public class DataFlowWatcher
{
    private readonly DataFlowConfig _config;
    private readonly ILogger<DataFlowWatcher> _logger;
    private readonly IWatchInbound _watcher;

    public DataFlowWatcher(ILogger<DataFlowWatcher> logger, DataFlowConfig config, IWatchInbound watcher)
    {
        _config = config;
        _logger = logger;
        _watcher = watcher;
    }

#if DEBUG
    // Local dev — blob trigger fires directly when a file lands in dataflow-source
    [Function("DataFlowWatcherLocal")]
    public async Task RunLocal(
        [BlobTrigger("dataflow-source/{blobName}")] BlobClient blobClient,
        string blobName,
        FunctionContext context
    )
    {
        blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));

        var cancellationToken = context.CancellationToken;

        // Pull the correlation ID from the blob request header
        string clientRequestId = await blobClient.GetCorrelationId();

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>() { { "CorrelationId", clientRequestId } }
            )
        )
        {
            _logger.LogInformation(
                DataFlowEvents.DataFlowWatcher.Id,
                "Received blob trigger for blob: {BlobName}",
                blobName
            );

            await _watcher.RunAsync(blobName, clientRequestId, cancellationToken);
        }
    }
#endif

    // Production — Event Grid trigger fires when a file lands in blob storage
    [Function(nameof(DataFlowWatcher))]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation("Received EventGridEvent: {@EventGridEvent}", eventGridEvent);
        var blobName = (eventGridEvent.Subject ?? string.Empty).Split('/').Last();

        string clientRequestId = eventGridEvent.Id;

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>() { { "CorrelationId", clientRequestId } }
            )
        )
        {
            _logger.LogInformation(
                DataFlowEvents.DataFlowWatcher.Id,
                "Received blob trigger for blob: {BlobName}",
                blobName
            );

            await _watcher.RunAsync(blobName, clientRequestId, cancellationToken);
        }
    }
}
