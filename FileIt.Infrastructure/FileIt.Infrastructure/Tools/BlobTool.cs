using Azure.Storage.Blobs;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Tools;

public class BlobTool : IHandleFiles
{
    private readonly ILogger<BlobTool> _logger;
    private readonly BlobServiceClient _blobServiceClient;

    public BlobTool(ILogger<BlobTool> logger, BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _blobServiceClient = blobServiceClient;
    }

    public async Task MoveAsync(
        string filename,
        string source,
        string destination,
        string? correlationId
    )
    {
        // Placeholder for moving a blob
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", correlationId ?? string.Empty },
                }
            )
        )
        {
            try
            {
                _logger.LogInformation(
                    InfrastructureEvents.BlobToolMoveStart,
                    "Moving Blob '{BlobName}' from {source} to {destination}",
                    filename,
                    source,
                    destination
                );
                var sourceContainerClient = _blobServiceClient.GetBlobContainerClient(source);
                var sourceExists = await sourceContainerClient.ExistsAsync();
                if (!sourceExists)
                    await sourceContainerClient.CreateIfNotExistsAsync();

                var destinationContainerClient = _blobServiceClient.GetBlobContainerClient(
                    destination
                );
                var destExists = await sourceContainerClient.ExistsAsync();
                if (!destExists)
                    await destinationContainerClient.CreateIfNotExistsAsync();

                var sourceBlobClient = sourceContainerClient.GetBlobClient(filename);
                var destinationBlobClient = destinationContainerClient.GetBlobClient(filename);

                var existsResponse = await sourceBlobClient.ExistsAsync();
                if (!existsResponse.Value)
                {
                    _logger.LogWarning(
                        InfrastructureEvents.BlobToolBlobNotFound,
                        "Blob '{BlobName}' not found in container '{SourceContainer}'",
                        filename,
                        source
                    );
                    return;
                }

                await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                await sourceBlobClient.DeleteAsync();

                _logger.LogInformation(
                    InfrastructureEvents.BlobToolMoved,
                    "Moved blob '{BlobName}' from '{SourceContainer}' to '{DestinationContainer}'",
                    filename,
                    source,
                    destination
                );
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(
                    InfrastructureEvents.BlobToolMoveFailed,
                    ex,
                    "Azure Storage request failed while moving blob '{BlobName}'",
                    filename
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    InfrastructureEvents.BlobToolUnexpected,
                    ex,
                    "Unexpected error while moving blob '{BlobName}'",
                    filename
                );
                throw;
            }
        }
    }

    public async Task GetFileAsync(string filename, string location, string? correlationId)
    {
        // Placeholder for getting a blob
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }

        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", correlationId ?? string.Empty },
                }
            )
        )
        {
            _logger.LogInformation(
                InfrastructureEvents.BlobToolGetFile,
                "Getting '{FileName}' from {Location}",
                filename,
                location
            );
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(location);
                var blobClient = containerClient.GetBlobClient(filename);

                var existsResponse = await blobClient.ExistsAsync();
                if (!existsResponse.Value)
                {
                    _logger.LogWarning(
                        "Blob '{BlobName}' not found in container '{Container}'",
                        filename,
                        location
                    );
                    return;
                }
                var downloadResponse = await blobClient.DownloadAsync();
                using var ms = new System.IO.MemoryStream();
                await downloadResponse.Value.Content.CopyToAsync(ms);
                ms.Position = 0;

                _logger.LogInformation(
                    "Downloaded blob '{BlobName}' from container '{Container}' ({Length} bytes)",
                    filename,
                    location,
                    ms.Length
                );
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(
                    ex,
                    "Azure Storage request failed while retrieving blob '{BlobName}'",
                    filename
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while retrieving blob '{BlobName}'",
                    filename
                );
                throw;
            }
        }
        await Task.CompletedTask;
    }

    public async Task UploadAsync(
        Stream content,
        string filename,
        string location,
        string? correlationId
    )
    {
        using (
            _logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "CorrelationId", correlationId ?? string.Empty },
                }
            )
        )
        {
            _logger.LogInformation(
                InfrastructureEvents.BlobToolUploadStart,
                "Uploading '{FileName}' from {Location}",
                filename,
                location
            );
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(location);
                await containerClient.UploadBlobAsync(filename, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    InfrastructureEvents.BlobToolUploadError,
                    ex,
                    "Error uploading {FileName}",
                    filename
                );
            }
            ;
        }
    }
}
