// This function listens on the dataflow-transform queue for files ready to transform.
// It runs the GL Account transform directly via Infrastructure, no Services involved.
// Output CSV goes to dataflow-final, request log gets updated with results.
using Azure.Messaging.ServiceBus;
using FileIt.Module.DataFlow.App;
using FileIt.Module.DataFlow.App.Transform;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.Host;

public class DataFlowSubscriber
{
    private readonly ILogger<DataFlowSubscriber> _logger;
    private readonly IDataFlowTransformProcessor _processor;

    public DataFlowSubscriber(
        ILogger<DataFlowSubscriber> logger,
        IDataFlowTransformProcessor processor
    )
    {
        _logger = logger;
        _processor = processor;
    }

    // Listens on the dataflow-transform queue for files ready to be transformed
    [Function(nameof(DataFlowSubscriber))]
    public async Task Run(
        [ServiceBusTrigger(DataFlowMessagingNames.DataFlowTransformQueue)]
            ServiceBusReceivedMessage message,
        FunctionContext context
    )
    {
        var cancellationToken = context.CancellationToken;
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
                DataFlowEvents.DataFlowSubscriberReceive,
                "Receiving message for correlation {CorrelationId}",
                clientRequestId
            );
            await _processor.RunAsync(clientRequestId, cancellationToken);
        }
    }
}
