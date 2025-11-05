using System;
using System.Threading.Tasks;
using FileIt.App.Models;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Providers
{
    public interface IBlobProvider
    {
        // Define methods for blob operations
        Task MoveBlobAsync(string name, string sOURCE_CONTAINER, string wORKING_CONTAINER);
    }

    public class BlobProvider : IBlobProvider
    {
        private readonly ILogger<BlobProvider> _logger;
        private readonly BlobServiceClient _blobServiceClient;

        public BlobProvider(ILogger<BlobProvider> logger, BlobServiceClient blobServiceClient)
        {
            // Initialize any required resources
            _logger = logger;
            _blobServiceClient = blobServiceClient;
        }

        // Implement blob operations here
        public async Task MoveBlobAsync(
            string blobName,
            string sourceContainer,
            string destinationContainer
        )
        {
            // Placeholder for moving a blob
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException("Blob name must be provided", nameof(blobName));
            }

            try
            {
                var sourceContainerClient = _blobServiceClient.GetBlobContainerClient(
                    sourceContainer
                );
                var destinationContainerClient = _blobServiceClient.GetBlobContainerClient(
                    destinationContainer
                );
                await destinationContainerClient.CreateIfNotExistsAsync();

                var sourceBlobClient = sourceContainerClient.GetBlobClient(blobName);
                var destinationBlobClient = destinationContainerClient.GetBlobClient(blobName);


                var existsResponse = await sourceBlobClient.ExistsAsync();
                if (!existsResponse.Value)
                {
                    _logger.LogWarning(
                        "Blob '{BlobName}' not found in container '{SourceContainer}'",
                        blobName,
                        sourceContainer
                    );
                    return;
                }

                await destinationBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri);
                await sourceBlobClient.DeleteAsync();

                _logger.LogInformation(
                    "Moved blob '{BlobName}' from '{SourceContainer}' to '{DestinationContainer}'",
                    blobName,
                    sourceContainer,
                    destinationContainer
                );
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(
                    ex,
                    "Azure Storage request failed while moving blob '{BlobName}'",
                    blobName
                );
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while moving blob '{BlobName}'", blobName);
                throw;
            }
        }

        public async Task GetBlobAsync(string name, string containerName)
        {
            // Placeholder for getting a blob
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Blob name must be provided", nameof(name));
            }

            try
            {
                // Assumes BlobProviderConfig exposes ConnectionString
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(name);

                var existsResponse = await blobClient.ExistsAsync();
                if (!existsResponse.Value)
                {
                    _logger.LogWarning(
                        "Blob '{BlobName}' not found in container '{Container}'",
                        name,
                        containerName
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
                    containerName,
                    ms.Length
                );

                // Example: process the blob bytes (ms.ToArray()) or pass the stream to other code.
                // var bytes = ms.ToArray();
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
    }
}
