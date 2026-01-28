using System.Net;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Extensions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FileIt.SimpleProvider.App;

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
        string clientRequestId = await blobClient.GetCorrelationId();

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", clientRequestId },
                    { "EventId", _config.SimpleIntakeEventId },
                }
            )
        )
        {
            _logger.LogInformation("Received blob trigger for blob: {BlobName}", blobName);

            await _watcher.RunAsync(blobName, clientRequestId);
        }
    }
}
