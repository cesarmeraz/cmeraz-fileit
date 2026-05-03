using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Infrastructure.Test.Tools;

[TestClass]
public class TestBlobTool
{
    public required Mock<ILogger<BlobTool>> _loggerMock;
    public required Mock<BlobServiceClient> _serviceClientMock;
    public required Mock<BlobContainerClient> _sourceContainerMock;
    public required Mock<BlobContainerClient> _destContainerMock;
    public required Mock<BlobClient> _sourceBlobMock;
    public required Mock<BlobClient> _destBlobMock;
    public required BlobTool target;

    private const string BlobName = "x.csv";
    private const string SourceContainer = "source";
    private const string DestContainer = "destination";

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<BlobTool>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );
        _serviceClientMock = new Mock<BlobServiceClient>();
        _sourceContainerMock = new Mock<BlobContainerClient>();
        _destContainerMock = new Mock<BlobContainerClient>();
        _sourceBlobMock = new Mock<BlobClient>();
        _destBlobMock = new Mock<BlobClient>();

        _serviceClientMock
            .Setup(s => s.GetBlobContainerClient(SourceContainer))
            .Returns(_sourceContainerMock.Object);
        _serviceClientMock
            .Setup(s => s.GetBlobContainerClient(DestContainer))
            .Returns(_destContainerMock.Object);
        _sourceContainerMock
            .Setup(c => c.GetBlobClient(BlobName))
            .Returns(_sourceBlobMock.Object);
        _destContainerMock
            .Setup(c => c.GetBlobClient(BlobName))
            .Returns(_destBlobMock.Object);

        // Container exists by default; tests can override.
        var trueResponse = Response.FromValue(true, Mock.Of<Response>());
        _sourceContainerMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trueResponse);
        _destContainerMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(trueResponse);

        target = new BlobTool(_loggerMock.Object, _serviceClientMock.Object);
    }

    private void SetupSuccessfulMove()
    {
        _sourceBlobMock
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var copyOpMock = new Mock<CopyFromUriOperation>();
        copyOpMock
            .Setup(o => o.WaitForCompletionAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<Response<long>>(Response.FromValue(0L, Mock.Of<Response>())));
        // BlobTool calls StartCopyFromUriAsync(Uri, cancellationToken: ct).
        // C# binds this to the (Uri, BlobCopyFromUriOptions, CancellationToken) overload
        // with options=null. Note that the signature uses BlobCopyFromUriOptions
        // (non-nullable), so the matcher must use the same type, not BlobCopyFromUriOptions?.
        _destBlobMock
            .Setup(b => b.StartCopyFromUriAsync(
                It.IsAny<Uri>(),
                It.IsAny<BlobCopyFromUriOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(copyOpMock.Object);

        _sourceBlobMock
            .Setup(b => b.DeleteAsync(
                It.IsAny<DeleteSnapshotsOption>(),
                It.IsAny<BlobRequestConditions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        _sourceBlobMock.SetupGet(b => b.Uri).Returns(new Uri("https://example.test/x.csv"));
    }

    [TestMethod]
    public async Task MoveAsync_EmptyBlobName_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.MoveAsync("", SourceContainer, DestContainer));
    }

    [TestMethod]
    public async Task MoveAsync_HappyPath_CallsCopyThenDelete()
    {
        SetupSuccessfulMove();

        await target.MoveAsync(BlobName, SourceContainer, DestContainer);

        _destBlobMock.Verify(b => b.StartCopyFromUriAsync(
            It.Is<Uri>(u => u.ToString().EndsWith("x.csv")),
            It.IsAny<BlobCopyFromUriOptions>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
        _sourceBlobMock.Verify(b => b.DeleteAsync(
            It.IsAny<DeleteSnapshotsOption>(),
            It.IsAny<BlobRequestConditions>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [TestMethod]
    public async Task MoveAsync_SourceBlobMissing_LogsWarningAndDoesNotCopy()
    {
        _sourceBlobMock
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        await target.MoveAsync(BlobName, SourceContainer, DestContainer);

        _destBlobMock.Verify(b => b.StartCopyFromUriAsync(
            It.IsAny<Uri>(),
            It.IsAny<BlobCopyFromUriOptions>(),
            It.IsAny<CancellationToken>()
        ), Times.Never);
        _sourceBlobMock.Verify(b => b.DeleteAsync(
            It.IsAny<DeleteSnapshotsOption>(),
            It.IsAny<BlobRequestConditions>(),
            It.IsAny<CancellationToken>()
        ), Times.Never);
    }

    [TestMethod]
    public async Task MoveAsync_SourceContainerMissing_CreatesIt()
    {
        _sourceContainerMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        SetupSuccessfulMove();

        await target.MoveAsync(BlobName, SourceContainer, DestContainer);

        _sourceContainerMock.Verify(c => c.CreateIfNotExistsAsync(
            It.IsAny<PublicAccessType>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<BlobContainerEncryptionScopeOptions>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [TestMethod]
    public async Task MoveAsync_DestinationContainerMissing_CreatesIt()
    {
        _destContainerMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));
        SetupSuccessfulMove();

        await target.MoveAsync(BlobName, SourceContainer, DestContainer);

        _destContainerMock.Verify(c => c.CreateIfNotExistsAsync(
            It.IsAny<PublicAccessType>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<BlobContainerEncryptionScopeOptions>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [TestMethod]
    public async Task MoveAsync_RequestFailed_RethrowsAfterLogging()
    {
        _sourceContainerMock
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(503, "Service Unavailable"));

        await Assert.ThrowsAsync<RequestFailedException>(
            () => target.MoveAsync(BlobName, SourceContainer, DestContainer));
    }

    [TestMethod]
    public async Task UploadAsync_NullStream_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => target.UploadAsync(null!, BlobName, DestContainer));
    }

    [TestMethod]
    public async Task UploadAsync_EmptyFilename_ThrowsArgumentException()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.UploadAsync(ms, "", DestContainer));
    }

    [TestMethod]
    public async Task UploadAsync_EmptyLocation_ThrowsArgumentException()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.UploadAsync(ms, BlobName, ""));
    }

    [TestMethod]
    public async Task DownloadAsync_EmptyFilename_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.DownloadAsync("", DestContainer));
    }

    [TestMethod]
    public async Task DownloadAsync_EmptyLocation_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.DownloadAsync(BlobName, ""));
    }

    [TestMethod]
    public async Task GetFileAsync_EmptyFilename_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => target.GetFileAsync("", DestContainer));
    }
}
