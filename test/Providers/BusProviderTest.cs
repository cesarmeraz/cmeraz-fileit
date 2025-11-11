using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FileIt.App.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FileIt.Tests.Providers
{
    [TestClass]
    public class BusProviderTest
    {
        private Mock<ILogger<BusProvider>> _loggerMock;
        private Mock<ServiceBusClient> _serviceBusClientMock;
        private Mock<ServiceBusSender> _serviceBusSenderMock;
        private BusProvider _busProvider;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<BusProvider>>();
            _serviceBusClientMock = new Mock<ServiceBusClient>();
            _serviceBusSenderMock = new Mock<ServiceBusSender>();

            _serviceBusClientMock
                .Setup(client => client.CreateSender(It.IsAny<string>()))
                .Returns(_serviceBusSenderMock.Object);

            _busProvider = new BusProvider(_loggerMock.Object, _serviceBusClientMock.Object);
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
            await Assert.ThrowsExceptionAsync<System.Exception>(
                () => _busProvider.SendMessageAsync(queueName, message)
            );

            
        }
    }
}
