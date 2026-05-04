using System.Text;
using Azure.Messaging.ServiceBus;
using FileIt.Module.Services.App;
using FileIt.Module.Services.App.ApiAdd;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.HttpClients;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Module.Services.App.Test.ApiAdd;

[TestClass]
public class TestApiFunc
{
    public required Mock<ILogger<ApiAddCommand>> _loggerMock;
    public required Mock<ServiceBusClient> _serviceBusClientMock;
    public required Mock<ServiceBusSender> _serviceBusSenderMock;
    public required Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
    public required Mock<IApiLogRepo> _apiLogRepoMock;
    public required Mock<IBroadcastResponses> _broadcasterMock;
    public required Mock<IComplexApiClient> _complexApiMock;
    public required ApiAddCommand target;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApiAddCommand>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()));
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        _serviceBusSenderMock = new Mock<ServiceBusSender>();
        _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        _apiLogRepoMock = new Mock<IApiLogRepo>();
        _broadcasterMock = new Mock<IBroadcastResponses>();
        _complexApiMock = new Mock<IComplexApiClient>();

        var config = new ServicesConfig()
        {
            ApiAddQueueName = "api-add",
            ApiAddTopicName = "api-add-topic",
        };
        _senderFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_serviceBusSenderMock.Object);
        _serviceBusClientMock
            .Setup(client => client.CreateSender(It.IsAny<string>()))
            .Returns(_serviceBusSenderMock.Object);
        _broadcasterMock
            .Setup(x => x.EmitAsync(It.IsAny<ApiAddResponse>()))
            .Returns(Task.CompletedTask);
        _complexApiMock
            .Setup(x => x.CreateDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexCreateResult(
                Guid.NewGuid(),
                "/api/documents/test",
                WasIdempotentReplay: false));

        target = new ApiAddCommand(
            _apiLogRepoMock.Object,
            _loggerMock.Object,
            _broadcasterMock.Object,
            _complexApiMock.Object
        );
    }

    private static ApiRequest BuildRequest(string? correlationId, string replyTo = "replyTo")
    {
        return new ApiRequest("test-message-id")
        {
            Body = "test body",
            ReplyTo = replyTo,
            Subject = "api-add-simple",
            CorrelationId = correlationId,
        };
    }

    [TestMethod]
    public async Task ApiAdd_HappyPath_PublishesAndStampsAuditRow()
    {
        // Arrange
        string clientRequestId = Guid.NewGuid().ToString();
        var complexId = Guid.NewGuid();
        _apiLogRepoMock
            .Setup(x => x.AddAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new ApiLog() { Id = 42 });
        _complexApiMock
            .Setup(x => x.CreateDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComplexCreateResult(complexId, "/api/documents/x", false));

        var request = BuildRequest(clientRequestId);

        // Act
        await target.ApiAdd(request);

        // Assert: ApiLog row stamped with Complex:<guid>
        _apiLogRepoMock.Verify(x => x.AddAsync(
            It.Is<string>(s => s == clientRequestId),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string>(s => s == $"Complex:{complexId}")
        ), Times.Once);

        // Assert: Broadcaster called with response carrying apiLogItem.Id
        _broadcasterMock.Verify(x => x.EmitAsync(
            It.Is<ApiAddResponse>(r =>
                r.NodeId == 42
                && r.CorrelationId == clientRequestId
                && r.TopicName == "replyTo"),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [TestMethod]
    public async Task ApiAdd_NullCorrelationId_StillPublishesWithGeneratedDocName()
    {
        // Arrange
        _apiLogRepoMock
            .Setup(x => x.AddAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new ApiLog() { Id = 1 });
        var request = BuildRequest(correlationId: null);

        // Act
        await target.ApiAdd(request);

        // Assert: Complex called once with a non-empty doc name even though CorrelationId was null
        _complexApiMock.Verify(x => x.CreateDocumentAsync(
            It.Is<string>(name => !string.IsNullOrWhiteSpace(name)),
            It.Is<string>(ct => ct == "application/json"),
            It.IsAny<string>(),
            It.Is<string?>(key => key == null),
            It.IsAny<CancellationToken>()
        ), Times.Once);

        // Assert: ApiLog row uses empty string for correlation id (per ApiAddCommand contract)
        _apiLogRepoMock.Verify(x => x.AddAsync(
            It.Is<string>(s => s == string.Empty),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);
    }

    [TestMethod]
    public async Task ApiAdd_ComplexUnavailable_BubblesExceptionForBrokerRetry()
    {
        // Arrange
        _complexApiMock
            .Setup(x => x.CreateDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ComplexApiUnavailableException("Complex 503"));
        var request = BuildRequest(Guid.NewGuid().ToString());

        // Act + Assert: exception must bubble so the Service Bus broker retries
        await Assert.ThrowsAsync<ComplexApiUnavailableException>(
            () => target.ApiAdd(request));
    }

    [TestMethod]
    public async Task ApiAdd_ComplexUnavailable_DoesNotWriteAuditRow()
    {
        // Arrange
        _complexApiMock
            .Setup(x => x.CreateDocumentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ComplexApiUnavailableException("Complex 503"));
        var request = BuildRequest(Guid.NewGuid().ToString());

        // Act
        try { await target.ApiAdd(request); } catch (ComplexApiUnavailableException) { }

        // Assert: audit log was NOT touched and broadcaster did NOT fire
        _apiLogRepoMock.Verify(x => x.AddAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Never);
        _broadcasterMock.Verify(x => x.EmitAsync(
            It.IsAny<ApiAddResponse>(),
            It.IsAny<CancellationToken>()
        ), Times.Never);
    }

    [TestMethod]
    public async Task ApiAdd_CancellationRequested_Throws()
    {
        // Arrange
        _apiLogRepoMock
            .Setup(x => x.AddAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new ApiLog() { Id = 1 });
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = BuildRequest(Guid.NewGuid().ToString());

        // Act + Assert: token already cancelled, should throw OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.ApiAdd(request, cts.Token));
    }

    [TestMethod]
    public async Task ApiAdd_PassesCorrelationIdAsIdempotencyKey()
    {
        // Arrange
        string clientRequestId = Guid.NewGuid().ToString();
        _apiLogRepoMock
            .Setup(x => x.AddAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(new ApiLog() { Id = 1 });
        var request = BuildRequest(clientRequestId);

        // Act
        await target.ApiAdd(request);

        // Assert: idempotency key matches correlation id so retries are safe
        _complexApiMock.Verify(x => x.CreateDocumentAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<string?>(key => key == clientRequestId),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}
