using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FileIt.App.Providers;
using FileIt.App.Services;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace FileIt.Tests.Services
{
    [TestClass]
    public class SimpleServiceTest
    {
        private Mock<ILogger<SimpleService>> _loggerMock;
        private Mock<IBlobProvider> _blobProviderMock;
        private Mock<IBusProvider> _busProviderMock;

        private SimpleService target;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<SimpleService>>();
            _blobProviderMock = new Mock<IBlobProvider>();
            _busProviderMock = new Mock<IBusProvider>();

            target = new SimpleService(
                _loggerMock.Object,
                _blobProviderMock.Object,
                _busProviderMock.Object
            );
        }

        [TestMethod]
        public async Task TestProcessAsync()
        {
            var blobName = "blob name";
            BinaryData messageBody = new BinaryData(
                System.Text.Encoding.UTF8.GetBytes("{\"Key\": \"Value\"}")
            );

            Dictionary<string, object> appProperties = new Dictionary<string, object>
            {
                { "BLOB_NAME", blobName },
            };

            ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: messageBody,
                messageId: "mockMessageId",
                partitionKey: "mockPartitionKey",
                sessionId: "mockSessionId",
                correlationId: "mockCorrelationId",
                subject: "mockSubject",
                to: "mockTo",
                contentType: "application/json",
                timeToLive: TimeSpan.FromMinutes(5),
                properties: appProperties
            );

            _blobProviderMock.Setup(x =>
                x.MoveBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            );

            await target.ProcessAsync(message);
        }

        [TestMethod]
        public async Task TestQueueAsync()
        {
            _loggerMock.Setup(m =>
                m.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(), // Represents the state object, often FormattedLogValues
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
                )
            );

            _blobProviderMock.Setup(x =>
                x.MoveBlobAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())
            );

            var blobName = "blob name";
            _busProviderMock.Setup(x =>
                x.SendMessageAsync(It.IsAny<string>(), It.IsAny<ServiceBusMessage>())
            );

            await target.QueueAsync(blobName);

            _blobProviderMock.Verify();
            _busProviderMock.Verify();
        }

        [TestMethod]
        public async Task TestValidateBlobAsync()
        {
            var result = await target.ValidateBlobAsync(null, null);

            Assert.IsTrue(result);
        }
    }
}
