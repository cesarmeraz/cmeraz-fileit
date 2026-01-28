using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Infrastructure.Tools
{
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
                        "Blob '{BlobName}' not found in container '{SourceContainer}'",
                        filename,
                        source
                    );
                    return;
                }

                await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                await sourceBlobClient.DeleteAsync();

                _logger.LogInformation(
                    "Moved blob '{BlobName}' from '{SourceContainer}' to '{DestinationContainer}'",
                    filename,
                    source,
                    destination
                );
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(
                    ex,
                    "Azure Storage request failed while moving blob '{BlobName}'",
                    filename
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while moving blob '{BlobName}'", filename);
                throw;
            }
        }

        public async Task GetFileAsync(string name, string location)
        {
            // Placeholder for getting a blob
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Blob name must be provided", nameof(name));
            }

            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient(location);
                var blobClient = containerClient.GetBlobClient(name);

                var existsResponse = await blobClient.ExistsAsync();
                if (!existsResponse.Value)
                {
                    _logger.LogWarning(
                        "Blob '{BlobName}' not found in container '{Container}'",
                        name,
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
                    name,
                    location,
                    ms.Length
                );
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(
                    ex,
                    "Azure Storage request failed while retrieving blob '{BlobName}'",
                    name
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving blob '{BlobName}'", name);
                throw;
            }
            await Task.CompletedTask;
        }

        public async Task UploadAsync(Stream content, string filename, string location)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(location);
            await containerClient.UploadBlobAsync(filename, content);
        }
    }
}
