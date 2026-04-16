// This function listens on the service bus topic for the transform completion message.
// When the Services function app finishes processing, it publishes a response here.
// We then update the request log and move the output file to the final container.
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Module.DataFlow.App;
using FileIt.Module.DataFlow.App.Transform;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.Host;

public class DataFlowSubscriber
{
    private readonly DataFlowConfig _config;
    private readonly ILogger<DataFlowSubscriber> _logger;
    private readonly ITransformGlAccounts _transformHandler;

    public DataFlowSubscriber(
        ILogger<DataFlowSubscriber> logger,
        DataFlowConfig config,
        ITransformGlAccounts transformHandler
    )
    {
        _config = config;
        _logger = logger;
        _transformHandler = transformHandler;
    }

    // Listens on the dataflow-transform-topic for responses from the Services app
    [Function(nameof(DataFlowSubscriber))]
    public async Task Run(
        [ServiceBusTrigger("dataflow-transform-topic", "dataflow-transform-sub")] ServiceBusReceivedMessage message
    )
    {
        string clientRequestId = message.CorrelationId ?? string.Empty;

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", clientRequestId ?? string.Empty },
                }
            )
        )
        {
            _logger.LogDebug(
                DataFlowEvents.DataFlowSubscriberReceive.Id,
                "Receiving {@message}",
                message
            );

            var response = JsonSerializer.Deserialize<ApiAddResponse>(message.Body.ToString());
            if (response == null)
            {
                _logger.LogWarning(
                    DataFlowEvents.DataFlowSubscriberReceiveFailed.Id,
                    "Failed to deserialize DataFlow response"
                );
                throw new ApplicationException("Failed to deserialize DataFlow response!");
            }

            _logger.LogInformation(
                DataFlowEvents.DataFlowSubscriber.Id,
                "Processing DataFlow response"
            );
        }
    }
}
