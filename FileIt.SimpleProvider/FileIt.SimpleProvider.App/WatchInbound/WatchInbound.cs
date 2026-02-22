using System.Text.Json;
using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Domain.Simple;
using Microsoft.Extensions.Logging;

namespace FileIt.SimpleProvider.App;

public interface IWatchInbound
{
    Task RunAsync(string blobName, string correlationId);
}

public class WatchInbound : IWatchInbound
{
    private readonly ILogger<WatchInbound> _logger;
    private readonly IHandleFiles _blobTool;
    private readonly ITalkToApi _busTool;
    private readonly SimpleConfig _config;
    private readonly ISimpleRequestLogRepo _requestLogRepo;

    public WatchInbound(
        ILogger<WatchInbound> logger,
        IHandleFiles blobTool,
        ITalkToApi busTool,
        ISimpleRequestLogRepo requestLogRepo,
        SimpleConfig config
    )
    {
        _blobTool = blobTool;
        _busTool = busTool;
        _config = config;
        _logger = logger;
        _requestLogRepo = requestLogRepo;
    }

    /// <summary>
    /// a BlobTrigger that receives the BlobClient and its name
    /// </summary>
    /// <param name="blobClient">the BlobClient</param>
    /// <param name="blobName">the file name</param>
    /// <returns></returns>
    public async Task RunAsync(string blobName, string correlationId)
    {
        // use the blobClient to get the x-ms-client-request-id property from the original request header
        string clientRequestId = correlationId;

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", clientRequestId },
                    { "EventId", SimpleEvents.SimpleWatcherCommand },
                }
            )
        )
        {
            _logger.LogInformation(
                "Received blob trigger for blob: {BlobName} with ClientRequestId: {CorrelationId}",
                blobName,
                clientRequestId
            );
            await _requestLogRepo.AddAsync(blobName, clientRequestId);

            await _blobTool.MoveAsync(
                blobName,
                _config.SourceContainer,
                _config.WorkingContainer,
                clientRequestId
            );

            string messageId = Guid.NewGuid().ToString();

            await _busTool.SendMessageAsync(
                new ApiRequest()
                {
                    Body = new ApiAddPayload() { FileName = blobName },
                    MessageId = messageId,
                    ReplyTo = _config.ApiAddTopicName,
                    CorrelationId = clientRequestId,
                    QueueName = _config.ApiAddQueueName,
                }
            );
            _logger.LogInformation("Message {MessageId} sent.", messageId);
        }
    }
}
