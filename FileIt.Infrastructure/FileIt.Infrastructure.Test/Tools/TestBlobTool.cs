using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileIt.Infrastructure.Test.Tools;

public class TestBlobTool
{
    [Test]
    public async Task TestMoveAsync()
    {
        var repository = new MockRepository();

        const string blobName = "simple-blob";
        const string sourceContainerName = "source";
        const string destinationContainerName = "destination";
        const string url = "https://example.com/source/simple-blob";

        var mockBlobServiceClient = repository.Of<BlobServiceClient>();
        var mockSourceBlobContainerClient = repository.Of<BlobContainerClient>();
        var mockDestBlobContainerClient = repository.Of<BlobContainerClient>();
        var mockSourceBlobClient = repository.Of<BlobClient>();
        var mockDestBlobClient = repository.Of<BlobClient>();
        var mockCopyFromUriOperation = repository.Of<CopyFromUriOperation>();
        var mockResponse = repository.Of<Response>();
        var mockDeleteResponse = repository.Of<Response>();

        var target = new BlobTool(NullLogger<BlobTool>.Instance, mockBlobServiceClient.Object);

        var mockBlobContainerInfo = BlobsModelFactory.BlobContainerInfo(
            new ETag("\"a1b2c3d4e5f6789\""),
            new DateTimeOffset(2025, 10, 27, 10, 0, 0, TimeSpan.Zero)
        );

        mockBlobServiceClient
            .GetBlobContainerClient(Arg.Is<string>(x => x == sourceContainerName))
            .Returns(mockSourceBlobContainerClient.Object);

        // mockSourceBlobContainerClient
        //     .CreateIfNotExistsAsync(
        //         Arg.Any<PublicAccessType>(),
        //         Arg.Any<IDictionary<string, string>>(),
        //         Arg.Any<BlobContainerEncryptionScopeOptions>(),
        //         Arg.Any<CancellationToken>()
        //     )
        //     .ReturnsAsync(
        //         Task.FromResult(Response.FromValue(mockBlobContainerInfo, mockResponse.Object))
        //     );

        mockSourceBlobContainerClient
            .ExistsAsync(Arg.Any<CancellationToken>())
            .ReturnsAsync(Task.FromResult(Response.FromValue<bool>(true, mockResponse.Object)));

        mockBlobServiceClient
            .GetBlobContainerClient(Arg.Is<string>(x => x == destinationContainerName))
            .Returns(mockDestBlobContainerClient.Object);

        // mockDestBlobContainerClient
        //     .CreateIfNotExistsAsync(
        //         Arg.Any<PublicAccessType>(),
        //         Arg.Any<IDictionary<string, string>>(),
        //         Arg.Any<BlobContainerEncryptionScopeOptions>(),
        //         Arg.Any<CancellationToken>()
        //     )
        //     .ReturnsAsync(
        //         Task.FromResult(Response.FromValue(mockBlobContainerInfo, mockResponse.Object))
        //     );

        mockDestBlobContainerClient
            .ExistsAsync(Arg.Any<CancellationToken>())
            .ReturnsAsync(Task.FromResult(Response.FromValue<bool>(true, mockResponse.Object)));

        mockSourceBlobContainerClient
            .GetBlobClient(Arg.Any<string>())
            .Returns(mockSourceBlobClient.Object);

        mockDestBlobContainerClient
            .GetBlobClient(Arg.Any<string>())
            .Returns(mockDestBlobClient.Object);

        mockDestBlobClient
            .StartCopyFromUriAsync(
                Arg.Any<Uri>(),
                Arg.Any<BlobCopyFromUriOptions>(),
                Arg.Any<CancellationToken>()
            )
            .ReturnsAsync(Task.FromResult(mockCopyFromUriOperation.Object));

        mockSourceBlobClient
            .ExistsAsync(Arg.Any<CancellationToken>())
            .ReturnsAsync(Task.FromResult(Response.FromValue<bool>(true, mockResponse.Object)));

        mockSourceBlobClient.Uri.Returns(new Uri(url));

        mockSourceBlobClient
            .DeleteAsync(
                Arg.Any<DeleteSnapshotsOption>(),
                Arg.Any<BlobRequestConditions>(),
                Arg.Any<CancellationToken>()
            )
            .ReturnsAsync(Task.FromResult(mockDeleteResponse.Object));

        await target.MoveAsync(blobName, sourceContainerName, destinationContainerName);

        mockBlobServiceClient.VerifyAll();
        mockSourceBlobContainerClient.VerifyAll();
        mockDestBlobContainerClient.VerifyAll();
        mockSourceBlobClient.VerifyAll();
        mockDestBlobClient.VerifyAll();
    }
}
