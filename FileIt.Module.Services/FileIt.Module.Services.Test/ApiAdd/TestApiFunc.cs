using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Module.Services.App.ApiAdd;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Module.Services.Test.ApiAdd;

[TestClass]
public class TestApiFunc
{
    public required Mock<ILogger<ApiAddCommand>> _loggerMock;
    public required Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
    public required Mock<IApiLogRepo> _apiLogRepoMock;
    public required Mock<IBroadcastResponses> _broadcasterMock;
    public required ApiAddCommand target;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApiAddCommand>>();
        _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        _apiLogRepoMock = new Mock<IApiLogRepo>();
        _broadcasterMock = new Mock<IBroadcastResponses>();

        _broadcasterMock
            .Setup(x => x.EmitAsync(It.IsAny<ApiAddResponse>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        target = new ApiAddCommand(
            _apiLogRepoMock.Object,
            _senderFactoryMock.Object,
            _loggerMock.Object,
            _broadcasterMock.Object
        );
    }

    [TestMethod]
    public async Task ApiAdd_HappyPath_StampsAuditRowAndBroadcastsResponse()
    {
        // Arrange
        string correlationId = Guid.NewGuid().ToString();
        const string replyTo = "replyTo";
        _apiLogRepoMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(new ApiLog() { Id = 42 });

        // Act
        await target.ApiAdd("msg-id", null, "api-add-simple", replyTo, correlationId, "body");

        // Assert: ApiLog row stamped with the correlationId
        _apiLogRepoMock.Verify(
            x =>
                x.AddAsync(
                    It.Is<string>(s => s == correlationId),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );

        // Assert: Broadcaster called with response carrying apiLogItem.Id
        _broadcasterMock.Verify(
            x =>
                x.EmitAsync(
                    It.Is<ApiAddResponse>(r =>
                        r.NodeId == 42 && r.CorrelationId == correlationId && r.TopicName == replyTo
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task ApiAdd_NullCorrelationId_UsesEmptyStringForAuditRow()
    {
        // Arrange
        _apiLogRepoMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(new ApiLog() { Id = 1 });

        // Act
        await target.ApiAdd("msg-id", null, "api-add-simple", "replyTo", null, "body");

        // Assert: ApiLog row uses empty string for correlation id (per ApiAddCommand contract)
        _apiLogRepoMock.Verify(
            x =>
                x.AddAsync(
                    It.Is<string>(s => s == string.Empty),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }
}
