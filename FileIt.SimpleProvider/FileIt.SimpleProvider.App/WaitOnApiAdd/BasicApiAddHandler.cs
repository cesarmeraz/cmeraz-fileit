using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Domain.Simple;
using FileIt.SimpleProvider;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.SimpleProvider.App.WaitOnApiUpload;

public interface IBasicApiAddHandler
{
    Task RunAsync(ApiAddResponse message);
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
    /// <returns></returns>
    public async Task RunAsync(ApiAddResponse message)
    {
        string clientRequestId = message.CorrelationId ?? string.Empty;

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", clientRequestId ?? string.Empty },
                    { "EventId", SimpleEvents.SimpleSubscriberCommand },
                }
            )
        )
        {
            _logger.LogInformation("Processing {@message}", message);

            SimpleRequestLog? entry = await _requestLogRepo.GetByClientRequestIdAsync(
                clientRequestId
            );
            if (entry == null)
            {
                _logger.LogError(
                    "No SimpleRequestLog found for ClientRequestId: {CorrelationId}",
                    clientRequestId
                );
                throw new Exception("SimpleRequestLog entry not found");
            }
            if (string.IsNullOrWhiteSpace(entry.BlobName))
            {
                _logger.LogError(
                    "No BlobName found for ClientRequestId: {CorrelationId}",
                    clientRequestId
                );
                throw new Exception("SimpleRequestLog entry is missing BlobName");
            }
            //Process the file then
            await _blobTool.MoveAsync(
                entry.BlobName,
                _config.WorkingContainer,
                _config.FinalContainer,
                clientRequestId
            );

            entry.ApiId = message.NodeId;
            await _requestLogRepo.UpdateAsync(entry);
            _logger.LogInformation("Processed Simple Request Log: {@entry}", entry);
        }
    }
}
