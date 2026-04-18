using System.Net.Sockets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure;
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

    public async Task MoveAsync(string filename, string source, string destination)
    {
        // Placeholder for moving a blob
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }
        try
        {
            _logger.LogInformation(
                InfrastructureEvents.BlobToolMoveStart.Id,
                "Moving Blob '{BlobName}' from {source} to {destination}",
                filename,
                source,
                destination
            );
            var sourceContainerClient = _blobServiceClient.GetBlobContainerClient(source);
            var sourceExists = await sourceContainerClient.ExistsAsync();
            if (!sourceExists)
                await sourceContainerClient.CreateIfNotExistsAsync();

            var destinationContainerClient = _blobServiceClient.GetBlobContainerClient(destination);
            var destExists = await destinationContainerClient.ExistsAsync();
            if (!destExists)
                await destinationContainerClient.CreateIfNotExistsAsync();

            var sourceBlobClient = sourceContainerClient.GetBlobClient(filename);
            var destinationBlobClient = destinationContainerClient.GetBlobClient(filename);

            var existsResponse = await sourceBlobClient.ExistsAsync();
            if (!existsResponse.Value)
            {
                _logger.LogWarning(
                    InfrastructureEvents.BlobToolBlobNotFound.Id,
                    "Blob '{BlobName}' not found in container '{SourceContainer}'",
                    filename,
                    source
                );
                return;
            }

            await destinationBlobClient.StartCopyFromUriAsync(
                sourceBlobClient.Uri,
                new BlobCopyFromUriOptions { AccessTier = AccessTier.Hot },
                CancellationToken.None
            );

            await sourceBlobClient.DeleteAsync();

            _logger.LogInformation(
                InfrastructureEvents.BlobToolMoved.Id,
                "Moved blob '{BlobName}' from '{SourceContainer}' to '{DestinationContainer}'",
                filename,
                source,
                destination
            );
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolMoveFailed.Id,
                ex,
                "Azure Storage request failed while moving blob '{BlobName}'",
                filename
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolUnexpected.Id,
                ex,
                "Unexpected error while moving blob '{BlobName}'",
                filename
            );
            throw;
        }
    }

    public async Task GetFileAsync(string filename, string location)
    {
        // Placeholder for getting a blob
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }

        _logger.LogInformation(
            InfrastructureEvents.BlobToolGetFile.Id,
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
                    InfrastructureEvents.BlobToolGetFileNotFound.Id,
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
                InfrastructureEvents.BlobToolGetFileDownloaded.Id,
                "Downloaded blob '{BlobName}' from container '{Container}' ({Length} bytes)",
                filename,
                location,
                ms.Length
            );
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolGetFileRequestFailed.Id,
                ex,
                "Azure Storage request failed while retrieving blob '{BlobName}'",
                filename
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolGetFileUnexpected.Id,
                ex,
                "Unexpected error while retrieving blob '{BlobName}'",
                filename
            );
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task UploadAsync(Stream content, string filename, string location)
    {
        _logger.LogInformation(
            InfrastructureEvents.BlobToolUploadStart.Id,
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
                InfrastructureEvents.BlobToolUploadError.Id,
                ex,
                "Error uploading {FileName}",
                filename
            );
        }
    }
}
