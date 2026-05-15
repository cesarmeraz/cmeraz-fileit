using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FileIt.Infrastructure.Test.Tools;

[TestClass]
public class TestBlobTool
{
    [TestMethod]
    public async Task TestMoveAsync()
    {
        var repository = new MockRepository(MockBehavior.Default);

        const string blobName = "simple-blob";
        const string sourceContainerName = "source";
        const string destinationContainerName = "destination";
        const string url = "https://example.com/source/simple-blob";

        var mockBlobServiceClient = repository.Create<BlobServiceClient>();
        var mockSourceBlobContainerClient = repository.Create<BlobContainerClient>();
        var mockDestBlobContainerClient = repository.Create<BlobContainerClient>();
        var mockSourceBlobClient = repository.Create<BlobClient>();
        var mockDestBlobClient = repository.Create<BlobClient>();
        var mockCopyFromUriOperation = repository.Create<CopyFromUriOperation>();
        var mockResponse = repository.Create<Response>();
        var mockDeleteResponse = repository.Create<Response>();

        var target = new BlobTool(NullLogger<BlobTool>.Instance, mockBlobServiceClient.Object);

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.Is<string>(s => s == sourceContainerName)))
            .Returns(mockSourceBlobContainerClient.Object);

        mockSourceBlobContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, mockResponse.Object));

        mockBlobServiceClient
            .Setup(x => x.GetBlobContainerClient(It.Is<string>(s => s == destinationContainerName)))
            .Returns(mockDestBlobContainerClient.Object);

        mockDestBlobContainerClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, mockResponse.Object));

        mockSourceBlobContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockSourceBlobClient.Object);

        mockDestBlobContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockDestBlobClient.Object);

        mockDestBlobClient
            .Setup(x =>
                x.StartCopyFromUriAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<BlobCopyFromUriOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(mockCopyFromUriOperation.Object);

        mockSourceBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, mockResponse.Object));

        mockSourceBlobClient.Setup(x => x.Uri).Returns(new Uri(url));

        mockSourceBlobClient
            .Setup(x =>
                x.DeleteAsync(
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(mockDeleteResponse.Object);

        await target.MoveAsync(blobName, sourceContainerName, destinationContainerName);

        repository.VerifyAll();
    }
}
