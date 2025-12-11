using System.Text;
using Azure.Messaging.ServiceBus;
using Castle.Core.Logging;
using FileIt.App.Api;
using FileIt.App.Functions.Api;
using FileIt.App.Repositories;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FileIt.Test.Api
{
    [TestClass]
    public class TestApiFunc
    {
        public required Mock<ILogger<ApiFunc>> _loggerMock;
        public required Mock<ServiceBusClient> _serviceBusClientMock;
        public required Mock<ServiceBusSender> _serviceBusSenderMock;
        public required Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
        public required Mock<IApiLogRepo> _apiLogRepoMock;
        public required ApiFunc target;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<ApiFunc>>();
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

            _senderFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(_serviceBusSenderMock.Object);
            _serviceBusClientMock
                .Setup(client => client.CreateSender(It.IsAny<string>()))
                .Returns(_serviceBusSenderMock.Object);
            target = new ApiFunc(
                _loggerMock.Object,
                _senderFactoryMock.Object,
                _apiLogRepoMock.Object
            );
        }

        [TestMethod]
        public void Test()
        {
            string replyTo = "replyTo";
            string subject = "api-add-simple";
            string clientRequestId = Guid.NewGuid().ToString();
            var messageBody = new BinaryData(
                Encoding.UTF8.GetBytes("This is a test message body.")
            );
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

            ServiceBusReceivedMessage mockMessage =
                ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: messageBody,
                    messageId: clientRequestId,
                    subject: subject,
                    contentType: "application/json",
                    replyTo: replyTo
                );
            target.ApiAdd(mockMessage);
        }
    }
}
