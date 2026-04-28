using System.Net.Sockets;
using Azure.Storage.Blobs;
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

    public async Task MoveAsync(string filename, string source, string destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }
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
            var sourceExists = await sourceContainerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!sourceExists)
                await sourceContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            var destinationContainerClient = _blobServiceClient.GetBlobContainerClient(destination);
            var destExists = await destinationContainerClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!destExists)
                await destinationContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            var sourceBlobClient = sourceContainerClient.GetBlobClient(filename);
            var destinationBlobClient = destinationContainerClient.GetBlobClient(filename);

            var existsResponse = await sourceBlobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
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

            var copyOperation = await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: cancellationToken).ConfigureAwait(false);
            await copyOperation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
            await sourceBlobClient.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

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

    public async Task GetFileAsync(string filename, string location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }

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

            var existsResponse = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!existsResponse.Value)
            {
                _logger.LogWarning(
                    InfrastructureEvents.BlobToolGetFileNotFound,
                    "Blob '{BlobName}' not found in container '{Container}'",
                    filename,
                    location
                );
                return;
            }
            var downloadResponse = await blobClient.DownloadAsync(cancellationToken).ConfigureAwait(false);
            using var ms = new System.IO.MemoryStream();
            await downloadResponse.Value.Content.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Position = 0;

            _logger.LogInformation(
                InfrastructureEvents.BlobToolGetFileDownloaded,
                "Downloaded blob '{BlobName}' from container '{Container}' ({Length} bytes)",
                filename,
                location,
                ms.Length
            );
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolGetFileRequestFailed,
                ex,
                "Azure Storage request failed while retrieving blob '{BlobName}'",
                filename
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolGetFileUnexpected,
                ex,
                "Unexpected error while retrieving blob '{BlobName}'",
                filename
            );
            throw;
        }
    }

    public async Task UploadAsync(Stream content, string filename, string location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location must be provided", nameof(location));
        }

        _logger.LogInformation(
            InfrastructureEvents.BlobToolUploadStart,
            "Uploading '{FileName}' to {Location}",
            filename,
            location
        );
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(location);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            var blobClient = containerClient.GetBlobClient(filename);
            await blobClient.UploadAsync(content, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolUploadError,
                ex,
                "Error uploading {FileName}",
                filename
            );
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string filename, string location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Blob name must be provided", nameof(filename));
        }
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location must be provided", nameof(location));
        }

        _logger.LogInformation(
            InfrastructureEvents.BlobToolGetFile,
            "Downloading '{FileName}' from {Location}",
            filename,
            location
        );

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(location);
            var blobClient = containerClient.GetBlobClient(filename);

            var downloadResponse = await blobClient.DownloadAsync(cancellationToken).ConfigureAwait(false);
            var ms = new MemoryStream();
            try
            {
                await downloadResponse.Value.Content.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                ms.Position = 0;
                return ms;
            }
            catch
            {
                await ms.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                InfrastructureEvents.BlobToolGetFileUnexpected,
                ex,
                "Error downloading '{FileName}' from {Location}",
                filename,
                location
            );
            throw;
        }
    }
}
