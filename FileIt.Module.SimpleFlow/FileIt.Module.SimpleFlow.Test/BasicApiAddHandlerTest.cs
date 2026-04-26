using System.Text.Json;
using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Module.SimpleFlow.App;
using FileIt.Module.SimpleFlow.App.WaitOnApiUpload;
using Microsoft.Extensions.Logging.Abstractions;
using TUnit.Core;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace FileIt.Module.SimpleFlow.Test;

public class BasicApiAddHandlerTest
{
    private Mock<IHandleFiles> _blobToolMock = null!;
    private Mock<ISimpleRequestLogRepo> _requestLogRepoMock = null!;
    private SimpleConfig _config = null!;
    private BasicApiAddHandler _handler = null!;

    public BasicApiAddHandlerTest()
    {
        var repository = new MockRepository();

        _requestLogRepoMock = repository.Of<ISimpleRequestLogRepo>();
        _blobToolMock = repository.Of<IHandleFiles>();

        _config = new SimpleConfig { WorkingContainer = "working", FinalContainer = "final" };

        _handler = new BasicApiAddHandler(
            NullLogger<BasicApiAddHandler>.Instance,
            _blobToolMock.Object,
            _requestLogRepoMock.Object,
            _config
        );
    }

    [Test]
    public async Task RunAsync_WithValidMessage_MovesFileAndUpdatesLog()
    {
        var correlationId = "test-correlation-id";
        var blobName = "test-blob";
        var nodeId = 123;
        var requestLog = new SimpleRequestLog
        {
            ClientRequestId = correlationId,
            BlobName = blobName,
        };
        _requestLogRepoMock.GetByClientRequestIdAsync(correlationId).Returns(requestLog);

        _blobToolMock
            .MoveAsync(blobName, _config.WorkingContainer, _config.FinalContainer)
            .Returns();

        _requestLogRepoMock
            .UpdateAsync(
                Arg.Is<SimpleRequestLog>(entry =>
                    entry is not null
                    && entry.ApiId == nodeId
                    && entry.BlobName is not null
                    && entry.BlobName == blobName
                ),
                Arg.Any<IDictionary<string, EntityOptions>?>()
            )
            .Returns(requestLog);

        var respose = new ApiAddResponse
        {
            CorrelationId = correlationId,
            NodeId = nodeId,
            TopicName = "test-topic",
            Subject = "test-subject",
            StatusCode = "200",
            Exception = null,
        };
        var messageBody = JsonSerializer.Serialize(respose);

        await _handler.RunAsync(correlationId, messageBody);

        _requestLogRepoMock.VerifyAll();
        _blobToolMock.VerifyAll();
    }

    [Test]
    public async Task RunAsync_WithNullRequestLog_ThrowsException()
    {
        var correlationId = "test-correlation-id";
        _requestLogRepoMock
            .GetByClientRequestIdAsync(correlationId)
            .Returns((SimpleRequestLog?)null);

        await Assert.ThrowsAsync<Exception>(() => _handler.RunAsync(correlationId, string.Empty));
    }

    [Test]
    public async Task RunAsync_WithMissingBlobName_ThrowsException()
    {
        var correlationId = "test-correlation-id";
        var requestLog = new SimpleRequestLog { ClientRequestId = correlationId, BlobName = null };

        _requestLogRepoMock.GetByClientRequestIdAsync(correlationId).Returns(requestLog);

        await Assert.ThrowsAsync<Exception>(() => _handler.RunAsync(correlationId, string.Empty));
    }

    [Test]
    public async Task RunAsync_WithNullCorrelationId_UsesEmptyString()
    {
        var blobName = "test-blob";
        var nodeId = 123;
        var requestLog = new SimpleRequestLog
        {
            ClientRequestId = string.Empty,
            BlobName = blobName,
        };
        _requestLogRepoMock.GetByClientRequestIdAsync(string.Empty).Returns(requestLog);

        _blobToolMock
            .MoveAsync(blobName, _config.WorkingContainer, _config.FinalContainer)
            .Returns();

        _requestLogRepoMock
            .UpdateAsync(
                Arg.Is<SimpleRequestLog>(entry => entry is not null && entry.ApiId == nodeId),
                Arg.Any<IDictionary<string, EntityOptions>?>()
            )
            .Returns(requestLog);

        var messageBody = JsonSerializer.Serialize(
            new ApiAddResponse
            {
                CorrelationId = string.Empty,
                NodeId = nodeId,
                TopicName = "test-topic",
                Subject = "test-subject",
                StatusCode = "200",
                Exception = null,
            }
        );

        await _handler.RunAsync(null, messageBody);

        _requestLogRepoMock.VerifyAll();
        _blobToolMock.VerifyAll();
    }
}
