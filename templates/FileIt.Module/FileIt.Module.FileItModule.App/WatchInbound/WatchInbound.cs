using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.FileItModule.App;

public interface IWatchInbound
{
    Task RunAsync(string blobName, string correlationId, CancellationToken cancellationToken = default);
}

public class WatchInbound : IWatchInbound
{
    private readonly ILogger<WatchInbound> _logger;
    private readonly IHandleFiles _blobTool;
    private readonly ITalkToApi _busTool;
    private readonly FileItModuleConfig _config;
    private readonly ISimpleRequestLogRepo _requestLogRepo;

    public WatchInbound(
        ILogger<WatchInbound> logger,
        IHandleFiles blobTool,
        ITalkToApi busTool,
        ISimpleRequestLogRepo requestLogRepo,
        FileItModuleConfig config)
    {
        _blobTool = blobTool;
        _busTool = busTool;
        _config = config;
        _logger = logger;
        _requestLogRepo = requestLogRepo;
    }

    /// <summary>
    /// Move the blob from source to working, write a RequestLog row, and
    /// enqueue an ApiRequest so the upstream API receives notification of the
    /// new file.
    /// </summary>
    public async Task RunAsync(string blobName, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            FileItModuleEvents.FileItModuleWatcherAddRequestLog,
            "Adding RequestLog for {BlobName}",
            blobName);
        await _requestLogRepo.AddAsync(blobName, correlationId);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            FileItModuleEvents.FileItModuleWatcherMoveToWorking,
            "Moving {BlobName} to working container",
            blobName);
        await _blobTool.MoveAsync(blobName, _config.SourceContainer, _config.WorkingContainer, cancellationToken);

        string messageId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            FileItModuleEvents.FileItModuleWatcherQueueApiAdd,
            "Add message for {BlobName} to API Add queue",
            blobName);
        await _busTool.SendMessageAsync(
            new ApiRequest(messageId)
            {
                Body = new ApiAddPayload() { FileName = blobName },
                ReplyTo = _config.ApiAddTopicName,
                CorrelationId = correlationId,
                QueueName = _config.ApiAddQueueName,
            },
            cancellationToken);
    }
}
