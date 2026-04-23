using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Module.SimpleFlow;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.SimpleFlow.App.WaitOnApiUpload;

public interface IBasicApiAddHandler
{
    Task RunAsync(ApiAddResponse message, CancellationToken cancellationToken = default);
}

public class BasicApiAddHandler : IBasicApiAddHandler
{
    private readonly ILogger<BasicApiAddHandler> _logger;
    private readonly ISimpleRequestLogRepo _requestLogRepo;
    private readonly IHandleFiles _blobTool;
    private readonly SimpleConfig _config;

    public BasicApiAddHandler(
        ILogger<BasicApiAddHandler> logger,
        IHandleFiles blobTool,
        ISimpleRequestLogRepo requestLogRepo,
        SimpleConfig config
    )
    {
        _blobTool = blobTool;
        _requestLogRepo = requestLogRepo;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// A ServiceBusTrigger that processes the file ingested
    /// </summary>
    /// <param name="message">the ServiceBusReceivedMessage</param>
    /// <param name="cancellationToken">token to observe for graceful cancellation</param>
    /// <returns></returns>
    public async Task RunAsync(ApiAddResponse message, CancellationToken cancellationToken = default)
    {
        string clientRequestId = message.CorrelationId ?? string.Empty;

        _logger.LogInformation(
            SimpleEvents.SimpleSubscriberGetRequestLog,
            "Get RequestLog by CorrelationId {CorrelationId}",
            message.CorrelationId
        );
        SimpleRequestLog? entry = await _requestLogRepo.GetByClientRequestIdAsync(clientRequestId);
        if (entry == null)
        {
            _logger.LogError(
                SimpleEvents.SimpleSubscriberRequestLogNotFound,
                "SimpleRequestLog entry not found"
            );
            throw new Exception("SimpleRequestLog entry not found");
        }
        if (string.IsNullOrWhiteSpace(entry.BlobName))
        {
            _logger.LogError(
                SimpleEvents.SimpleSubscriberBlobNameMissing,
                "SimpleRequestLog entry is missing BlobName"
            );
            throw new Exception("SimpleRequestLog entry is missing BlobName");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            SimpleEvents.SimpleSubscriberMoveToFinal,
            "Moving {BlobName} to Final",
            entry.BlobName
        );
        await _blobTool.MoveAsync(entry.BlobName, _config.WorkingContainer, _config.FinalContainer, cancellationToken);

        entry.ApiId = message.NodeId;

        _logger.LogInformation(
            SimpleEvents.SimpleSubscriberUpdateRequestLog,
            "Update RequestLog with {ApiId}",
            entry.ApiId
        );
        await _requestLogRepo.UpdateAsync(entry);

        _logger.LogDebug(
            SimpleEvents.SimpleSubscriberCompleted,
            "Processed Simple Request Log: {@entry}",
            entry
        );
    }
}
