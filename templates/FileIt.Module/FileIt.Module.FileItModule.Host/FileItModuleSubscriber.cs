using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Module.FileItModule.App;
using FileIt.Module.FileItModule.App.WaitOnApiUpload;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.FileItModule;

/// <summary>
/// Service Bus topic subscriber. Listens on api-add-topic, subscription
/// api-add-fileitmodule-sub, hands the response to the BasicApiAddHandler.
/// </summary>
public class FileItModuleSubscriber
{
    private readonly FileItModuleConfig _config;
    private readonly ILogger<FileItModuleSubscriber> _logger;
    private readonly IBasicApiAddHandler _responseHandler;

    public FileItModuleSubscriber(
        ILogger<FileItModuleSubscriber> logger,
        FileItModuleConfig config,
        IBasicApiAddHandler responseHandler)
    {
        _config = config;
        _logger = logger;
        _responseHandler = responseHandler;
    }

    [Function(nameof(FileItModuleSubscriber))]
    public async Task Run(
        [ServiceBusTrigger("api-add-topic", "api-add-fileitmodule-sub")]
            ServiceBusReceivedMessage message,
        FunctionContext context)
    {
        var cancellationToken = context.CancellationToken;
        string clientRequestId = message.CorrelationId ?? string.Empty;

        using (_logger.BeginScope(
            new Dictionary<string, object>()
            {
                { "CorrelationId", clientRequestId ?? string.Empty },
            }))
        {
            _logger.LogDebug(
                FileItModuleEvents.FileItModuleSubscriberReceive,
                "Receiving {@message}",
                message);

            var response = JsonSerializer.Deserialize<ApiAddResponse>(message.Body.ToString());
            if (response == null)
            {
                _logger.LogWarning(
                    FileItModuleEvents.FileItModuleSubscriberReceiveFailed,
                    "Failed to deserialize ApiAddResponse");
                throw new ApplicationException("Failed to deserialize ApiAddResponse!");
            }

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                FileItModuleEvents.FileItModuleSubscriber,
                "Processing ApiAddResponse");
            await _responseHandler.RunAsync(response, cancellationToken);
        }
    }
}
