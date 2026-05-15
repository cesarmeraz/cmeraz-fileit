using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.FileItModule.App.WaitOnApiUpload;

public interface IBasicApiAddHandler
{
    Task RunAsync(ApiAddResponse message, CancellationToken cancellationToken = default);
}

public class BasicApiAddHandler : IBasicApiAddHandler
{
    private readonly ILogger<BasicApiAddHandler> _logger;
    private readonly ISimpleRequestLogRepo _requestLogRepo;
    private readonly IHandleFiles _blobTool;
    private readonly FileItModuleConfig _config;

    public BasicApiAddHandler(
        ILogger<BasicApiAddHandler> logger,
        IHandleFiles blobTool,
        ISimpleRequestLogRepo requestLogRepo,
        FileItModuleConfig config)
    {
        _blobTool = blobTool;
        _requestLogRepo = requestLogRepo;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Handle the API's response: look up the request log by correlation id,
    /// move the blob to the final container, and update the request log with
    /// the API node id returned by the upstream service.
    /// </summary>
    public async Task RunAsync(ApiAddResponse message, CancellationToken cancellationToken = default)
    {
        string clientRequestId = message.CorrelationId ?? string.Empty;

        _logger.LogInformation(
            FileItModuleEvents.FileItModuleSubscriberGetRequestLog,
            "Get RequestLog by CorrelationId {CorrelationId}",
            message.CorrelationId);
        SimpleRequestLog? entry = await _requestLogRepo.GetByClientRequestIdAsync(clientRequestId);
        if (entry == null)
        {
            _logger.LogError(
                FileItModuleEvents.FileItModuleSubscriberRequestLogNotFound,
                "SimpleRequestLog entry not found");
            throw new Exception("SimpleRequestLog entry not found");
        }
        if (string.IsNullOrWhiteSpace(entry.BlobName))
        {
            _logger.LogError(
                FileItModuleEvents.FileItModuleSubscriberBlobNameMissing,
                "SimpleRequestLog entry is missing BlobName");
            throw new Exception("SimpleRequestLog entry is missing BlobName");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            FileItModuleEvents.FileItModuleSubscriberMoveToFinal,
            "Moving {BlobName} to Final",
            entry.BlobName);
        await _blobTool.MoveAsync(entry.BlobName, _config.WorkingContainer, _config.FinalContainer, cancellationToken);

        entry.ApiId = message.NodeId;

        _logger.LogInformation(
            FileItModuleEvents.FileItModuleSubscriberUpdateRequestLog,
            "Update RequestLog with {ApiId}",
            entry.ApiId);
        await _requestLogRepo.UpdateAsync(entry);

        _logger.LogDebug(
            FileItModuleEvents.FileItModuleSubscriberCompleted,
            "Processed Simple Request Log: {@entry}",
            entry);
    }
}
