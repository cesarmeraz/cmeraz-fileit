using System.Text;
using Azure.Messaging.ServiceBus;
using FileIt.Module.Services.App;
using FileIt.Module.Services.App.ApiAdd;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
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
    public required ApiAddCommand target;

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApiAddCommand>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(), // Represents the state object, often an anonymous type for structured logging
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>() // The formatter function
            )
        );
        _loggerMock.Setup(x => x.BeginScope(It.IsAny<Dictionary<string, object>>()));
        _serviceBusClientMock = new Mock<ServiceBusClient>();
        _serviceBusSenderMock = new Mock<ServiceBusSender>();
        _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
        _apiLogRepoMock = new Mock<IApiLogRepo>();
        _broadcasterMock = new Mock<IBroadcastResponses>();

        var config = new ServicesConfig()
        {
            ApiAddQueueName = "api-add",
            ApiAddTopicName = "api-add-topic",
        };
        var response = new ApiAddResponse()
        {
            NodeId = 1,
            CorrelationId = Guid.NewGuid().ToString(),
            TopicName = config.ApiAddTopicName,
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
        target = new ApiAddCommand(
            _apiLogRepoMock.Object,
            _senderFactoryMock.Object,
            _loggerMock.Object,
            _broadcasterMock.Object
        );
    }

    [TestMethod]
    public async Task Test()
    {
        string replyTo = "replyTo";
        string subject = "api-add-simple";
        string clientRequestId = Guid.NewGuid().ToString();
        var messageBody = new BinaryData(Encoding.UTF8.GetBytes("This is a test message body."));
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
        var messageId = "test-message-id";
        var mockMessage = new ApiRequest(messageId)
        {
            Body = messageBody.ToString(),
            ReplyTo = replyTo,
            Subject = subject,
            CorrelationId = clientRequestId,
        };

        await target.ApiAdd(mockMessage);
        _apiLogRepoMock.Verify(x =>
            x.AddAsync(
                It.Is<string>(s => s == clientRequestId),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            )
        );
        _broadcasterMock.Verify(x => x.EmitAsync(It.IsAny<ApiAddResponse>()), Times.Once);
    }
}
