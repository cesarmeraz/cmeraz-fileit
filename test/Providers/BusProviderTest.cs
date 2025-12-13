using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FileIt.App.Providers;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FileIt.Test.Providers
{
    [TestClass]
    public class BusProviderTest
    {
        public required Mock<ILogger<BusTool>> _loggerMock;
        public required Mock<ServiceBusClient> _serviceBusClientMock;
        public required Mock<ServiceBusSender> _serviceBusSenderMock;
        public required Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
        public required BusTool _busProvider;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<BusTool>>();
            _serviceBusClientMock = new Mock<ServiceBusClient>();
            _serviceBusSenderMock = new Mock<ServiceBusSender>();
            _senderFactoryMock = new Mock<IAzureClientFactory<ServiceBusSender>>();
            _senderFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(_serviceBusSenderMock.Object);
            _serviceBusClientMock
                .Setup(client => client.CreateSender(It.IsAny<string>()))
                .Returns(_serviceBusSenderMock.Object);

            _busProvider = new BusTool(_loggerMock.Object, _senderFactoryMock.Object);
        }

        [TestMethod]
        public async Task SendMessageAsync_ShouldSendMessage_WhenCalled()
        {
            // Arrange
            var queueName = "testQueue";
            var message = new ServiceBusMessage("Test message");

            // Act
            await _busProvider.SendMessageAsync(queueName, message);

            // Assert
            _serviceBusSenderMock.Verify(
                sender => sender.SendMessageAsync(message, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [TestMethod]
        public async Task SendMessageAsync_ShouldLogError_WhenExceptionThrown()
        {
            // Arrange
            var queueName = "testQueue";
            var message = new ServiceBusMessage("Test message");
            _serviceBusSenderMock
                .Setup(sender => sender.SendMessageAsync(message, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Send failed"));

            // Act
            await Assert.ThrowsExceptionAsync<System.Exception>(() =>
                _busProvider.SendMessageAsync(queueName, message)
            );
        }
    }
}
