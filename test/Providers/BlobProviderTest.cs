using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileIt.App.Providers;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Test.Providers;

[TestClass]
public class BlobProviderTest
{
    [TestMethod]
    public void TestMoveBlobAsync()
    {
        //parameters
        string blobName = "simple-blob";
        string sourceContainerName = "source";
        string destinationContainerName = "destination";
        // Create a mock BlobContainerInfo
        ETag mockETag = new ETag("\"a1b2c3d4e5f6789\"");
        DateTimeOffset mockLastModified = new DateTimeOffset(2025, 10, 27, 10, 0, 0, TimeSpan.Zero);
        BlobContainerInfo mockBlobContainerInfo = BlobsModelFactory.BlobContainerInfo(
            mockETag,
            mockLastModified
        );

        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var mockLogger = new Mock<ILogger<BlobProvider>>();
        IBlobProvider target = new BlobProvider(mockLogger.Object, mockBlobServiceClient.Object);

        var mockSourceBlobContainerClient = new Mock<BlobContainerClient>();
        mockBlobServiceClient
            .Setup(bsc =>
                bsc.GetBlobContainerClient(It.Is<string>(x => x.Equals(sourceContainerName)))
            )
            .Returns(mockSourceBlobContainerClient.Object);

        mockSourceBlobContainerClient
            .Setup(client =>
                client.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<System.Collections.Generic.IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<System.Threading.CancellationToken>()
                )
            )
            .ReturnsAsync(
                Response.FromValue(
                    mockBlobContainerInfo, // Example BlobContainerInfo
                    Mock.Of<Azure.Response>() // Mock a basic Azure.Response
                )
            );

        var mockDestBlobContainerClient = new Mock<BlobContainerClient>();
        mockBlobServiceClient
            .Setup(bsc =>
                bsc.GetBlobContainerClient(It.Is<string>(x => x.Equals(destinationContainerName)))
            )
            .Returns(mockDestBlobContainerClient.Object);

        mockDestBlobContainerClient
            .Setup(client =>
                client.CreateIfNotExistsAsync(
                    It.IsAny<PublicAccessType>(),
                    It.IsAny<System.Collections.Generic.IDictionary<string, string>>(),
                    It.IsAny<BlobContainerEncryptionScopeOptions>(),
                    It.IsAny<System.Threading.CancellationToken>()
                )
            )
            .ReturnsAsync(
                Response.FromValue(
                    mockBlobContainerInfo, // Example BlobContainerInfo
                    Mock.Of<Azure.Response>() // Mock a basic Azure.Response
                )
            );

        var mockSourceBlobClient = new Mock<BlobClient>();
        mockSourceBlobContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockSourceBlobClient.Object);
        var mockCopyFromUriOperation = new Mock<CopyFromUriOperation>();
        var mockDestBlobClient = new Mock<BlobClient>();
        mockDestBlobClient
            .Setup(x =>
                x.StartCopyFromUriAsync(
                    It.IsAny<Uri>(),
                    It.IsAny<BlobCopyFromUriOptions>(),
                    It.IsAny<System.Threading.CancellationToken>()
                )
            )
            .Returns(Task.FromResult(mockCopyFromUriOperation.Object));

        mockDestBlobContainerClient
            .Setup(x => x.GetBlobClient(It.IsAny<string>()))
            .Returns(mockDestBlobClient.Object);

        mockSourceBlobClient
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, new Mock<Response>().Object));

        var mockDeleteAzureResponse = new Mock<Azure.Response>();
        mockSourceBlobClient
            .Setup(x =>
                x.DeleteAsync(
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(mockDeleteAzureResponse.Object);

        mockLogger.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(), // Represents the state object, often an anonymous type for structured logging
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>() // The formatter function
            )
        );

        target.MoveBlobAsync(blobName, sourceContainerName, destinationContainerName);

        //verify all
        // mockBlobServiceClient.VerifyAll();
        // mockSourceBlobContainerClient.VerifyAll();
        // mockDestBlobContainerClient.VerifyAll();
        // mockSourceBlobClient.VerifyAll();
        // mockDestBlobClient.VerifyAll();
        // mockLogger.VerifyAll();
    }
}
