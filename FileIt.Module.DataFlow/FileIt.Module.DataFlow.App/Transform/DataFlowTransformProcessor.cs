using System.Text;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.DataFlow.App.Transform;

public interface IDataFlowTransformProcessor
{
    Task RunAsync(string clientRequestId, CancellationToken cancellationToken = default);
}

public class DataFlowTransformProcessor : IDataFlowTransformProcessor
{
    private readonly DataFlowConfig _config;
    private readonly ILogger<DataFlowTransformProcessor> _logger;
    private readonly ITransformGlAccounts _transformHandler;
    private readonly IDataFlowRequestLogRepo _requestLogRepo;
    private readonly IHandleFiles _blobTool;

    public DataFlowTransformProcessor(
        ILogger<DataFlowTransformProcessor> logger,
        DataFlowConfig config,
        ITransformGlAccounts transformHandler,
        IDataFlowRequestLogRepo requestLogRepo,
        IHandleFiles blobTool
    )
    {
        _config = config;
        _logger = logger;
        _transformHandler = transformHandler;
        _requestLogRepo = requestLogRepo;
        _blobTool = blobTool;
    }

    public async Task RunAsync(
        string clientRequestId,
        CancellationToken cancellationToken = default
    )
    {
        // Look up the request log to get the blob name
        _logger.LogInformation(
            DataFlowEvents.DataFlowSubscriberGetRequestLog,
            "Looking up RequestLog for correlation {CorrelationId}",
            clientRequestId
        );

        var entry = await _requestLogRepo.GetByClientRequestIdAsync(clientRequestId);
        if (entry == null)
        {
            _logger.LogError(
                DataFlowEvents.DataFlowSubscriberRequestLogNotFound,
                "DataFlowRequestLog not found for correlation {CorrelationId}",
                clientRequestId
            );
            throw new ApplicationException("DataFlowRequestLog entry not found");
        }

        if (string.IsNullOrWhiteSpace(entry.BlobName))
        {
            _logger.LogError(
                DataFlowEvents.DataFlowSubscriberBlobNameMissing,
                "BlobName is missing from DataFlowRequestLog"
            );
            throw new ApplicationException("DataFlowRequestLog entry is missing BlobName");
        }

        // Download the CSV from working container
        var csvStream = await _blobTool.DownloadAsync(
            entry.BlobName,
            _config.WorkingContainer,
            cancellationToken
        );

        cancellationToken.ThrowIfCancellationRequested();

        // Run the transform
        _logger.LogInformation(
            DataFlowEvents.DataFlowTransform,
            "Running GL Account transform for {BlobName}",
            entry.BlobName
        );

        string outputCsv = await _transformHandler.RunAsync(
            csvStream,
            clientRequestId,
            cancellationToken
        );

        cancellationToken.ThrowIfCancellationRequested();

        // Count rows in output
        var outputLines = outputCsv.Split(
            Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries
        );
        int rowsTransformed = outputLines.Length - 1;

        // Upload output CSV to final container
        string exportBlobName = $"summary_{entry.BlobName}";
        using var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(outputCsv));
        await _blobTool.UploadAsync(
            outputStream,
            exportBlobName,
            _config.FinalContainer,
            cancellationToken
        );

        _logger.LogInformation(
            DataFlowEvents.DataFlowSubscriberMoveToFinal,
            "Uploaded output file {ExportBlobName} to final container",
            exportBlobName
        );

        // Update the request log with transform results directly
        await _requestLogRepo.UpdateTransformResultAsync(
            clientRequestId,
            rowsTransformed,
            exportBlobName,
            "Complete"
        );

        _logger.LogInformation(
            DataFlowEvents.DataFlowSubscriberCompleted,
            "DataFlow transform complete. {RowsTransformed} groups written to {ExportBlobName}",
            rowsTransformed,
            exportBlobName
        );
    }
}
