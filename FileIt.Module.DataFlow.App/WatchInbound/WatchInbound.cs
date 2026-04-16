// This is the entry point for the DataFlow module.
// It gets triggered when a new GL Account CSV file lands in the source blob container.
// It does three things: logs the incoming file, moves it to working, 
// and puts a message on the service bus to kick off the transform.
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.App.WatchInbound;

public interface IWatchInbound
{
    Task RunAsync(string blobName, string correlationId);
}

public class WatchInbound : IWatchInbound
{
    private readonly ILogger<WatchInbound> _logger;
    private readonly IHandleFiles _blobTool;
    private readonly ITalkToApi _busTool;
    private readonly DataFlowConfig _config;
    private readonly IDataFlowRequestLogRepo _requestLogRepo;

    public WatchInbound(
        ILogger<WatchInbound> logger,
        IHandleFiles blobTool,
        ITalkToApi busTool,
        IDataFlowRequestLogRepo requestLogRepo,
        DataFlowConfig config
    )
    {
        _blobTool = blobTool;
        _busTool = busTool;
        _config = config;
        _logger = logger;
        _requestLogRepo = requestLogRepo;
    }

    public async Task RunAsync(string blobName, string correlationId)
    {
        // Step 1 - write a record to the database so we can trace this file through the whole flow
        _logger.LogInformation(
            DataFlowEvents.DataFlowWatcherAddRequestLog.Id,
            "Adding RequestLog for {BlobName}",
            blobName
        );
        await _requestLogRepo.AddAsync(blobName, correlationId);

        // Step 2 - move the file out of source into working so nothing else picks it up
        _logger.LogInformation(
            DataFlowEvents.DataFlowWatcherMoveToWorking.Id,
            "Moving {BlobName} to working container",
            blobName
        );
        await _blobTool.MoveAsync(blobName, _config.SourceContainer, _config.WorkingContainer);

        // Step 3 - put a message on the service bus queue so the transform handler knows there's work to do
        string messageId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            DataFlowEvents.DataFlowWatcherQueueTransform.Id,
            "Queuing transform message for {BlobName}",
            blobName
        );
        await _busTool.SendMessageAsync(
            new Domain.Entities.Api.ApiRequest(messageId)
            {
                Body = new DataFlowMessage() { BlobName = blobName },
                ReplyTo = _config.TransformTopicName,
                CorrelationId = correlationId,
                QueueName = _config.TransformQueueName,
            }
        );
    }
}
