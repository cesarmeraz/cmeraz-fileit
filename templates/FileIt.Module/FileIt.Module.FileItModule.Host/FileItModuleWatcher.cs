using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using FileIt.Infrastructure.Extensions;
using FileIt.Module.FileItModule.App;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.EventGrid;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.FileItModule;

/// <summary>
/// Entry point for new blobs landing in the source container. Local dev gets a
/// BlobTrigger (no EventGrid emulator), Azure gets the EventGridTrigger.
/// </summary>
public class FileItModuleWatcher
{
    private readonly FileItModuleConfig _config;
    private readonly ILogger<FileItModuleWatcher> _logger;
    private readonly IWatchInbound _watcher;

    public FileItModuleWatcher(
        ILogger<FileItModuleWatcher> logger,
        FileItModuleConfig config,
        IWatchInbound watcher)
    {
        _config = config;
        _logger = logger;
        _watcher = watcher;
    }

#if DEBUG
    [Function("FileItModuleWatcherLocal")]
    public async Task RunLocal(
        [BlobTrigger("fileitmodule-source/{blobName}")] BlobClient blobClient,
        string blobName,
        FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;
        ArgumentNullException.ThrowIfNull(blobClient);

        string clientRequestId = await blobClient.GetCorrelationId();

        using (_logger.BeginScope(
            new Dictionary<string, object>() { { "CorrelationId", clientRequestId } }))
        {
            _logger.LogInformation(
                FileItModuleEvents.FileItModuleWatcher,
                "Received blob trigger for blob: {BlobName}",
                blobName);

            cancellationToken.ThrowIfCancellationRequested();
            await _watcher.RunAsync(blobName, clientRequestId, cancellationToken);
        }
    }
#endif

    [Function(nameof(FileItModuleWatcher))]
    public async Task Run(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation("Received EventGridEvent: {@EventGridEvent}", eventGridEvent);
        var blobName = (eventGridEvent.Subject ?? string.Empty).Split('/').Last();

        string clientRequestId = eventGridEvent.Id;

        using (_logger.BeginScope(
            new Dictionary<string, object>() { { "CorrelationId", clientRequestId } }))
        {
            _logger.LogInformation(
                FileItModuleEvents.FileItModuleWatcher,
                "Received blob trigger for blob: {BlobName}",
                blobName);

            cancellationToken.ThrowIfCancellationRequested();
            await _watcher.RunAsync(blobName, clientRequestId, cancellationToken);
        }
    }
}
