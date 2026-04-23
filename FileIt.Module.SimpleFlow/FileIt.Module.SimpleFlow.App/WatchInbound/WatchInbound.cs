using System.Text.Json;
using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.SimpleFlow.App;

public interface IWatchInbound
{
    Task RunAsync(string blobName, string correlationId, CancellationToken cancellationToken = default);
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
    /// <param name="blobName">the file name</param>
    /// <param name="correlationId">the correlation id for this run</param>
    /// <param name="cancellationToken">token to observe for graceful cancellation</param>
    /// <returns></returns>
    public async Task RunAsync(string blobName, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            SimpleEvents.SimpleWatcherAddRequestLog.Id,
            "Adding RequestLog for {BlobName}",
            blobName
        );
        await _requestLogRepo.AddAsync(blobName, correlationId);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            SimpleEvents.SimpleWatcherMoveToWorking.Id,
            "Adding RequestLog for {BlobName}",
            blobName
        );
        await _blobTool.MoveAsync(blobName, _config.SourceContainer, _config.WorkingContainer, cancellationToken);

        string messageId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            SimpleEvents.SimpleWatcherQueueApiAdd.Id,
            "Add message for {BlobName} to API Add queue",
            blobName
        );
        await _busTool.SendMessageAsync(
            new ApiRequest(messageId)
            {
                Body = new ApiAddPayload() { FileName = blobName },
                ReplyTo = _config.ApiAddTopicName,
                CorrelationId = correlationId,
                QueueName = _config.ApiAddQueueName,
            },
            cancellationToken
        );
    }
}
