using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FileIt.Infrastructure.Test.Tools;

[TestClass]
public class TestBusTool
{
    private Mock<ServiceBusSender> _serviceBusSenderMock = null!;
    private Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock = null!;
    private BusTool _busProvider = null!;

    [TestInitialize]
    public void SetUp()
    {
        var repository = new MockRepository(MockBehavior.Default);

        _serviceBusSenderMock = repository.Create<ServiceBusSender>();
        _senderFactoryMock = repository.Create<IAzureClientFactory<ServiceBusSender>>();

        _senderFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(_serviceBusSenderMock.Object);

        _busProvider = new BusTool(NullLogger<BusTool>.Instance, _senderFactoryMock.Object);
    }

    [TestMethod]
    public async Task SendMessageAsync_ShouldSendMessage_WhenCalled()
    {
        var messageId = "test-message-id";
        var request = new ApiRequest(messageId) { Body = "Test message", QueueName = "testqueue" };

        _serviceBusSenderMock
            .Setup(x =>
                x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>())
            )
            .Returns(Task.CompletedTask);

        await _busProvider.SendMessageAsync(request);

        _serviceBusSenderMock.VerifyAll();
    }

    [TestMethod]
    public async Task SendMessageAsync_ShouldThrow_WhenSendFails()
    {
        var messageId = "test-message-id";
        var request = new ApiRequest(messageId) { Body = "Test message", QueueName = "testqueue" };

        _serviceBusSenderMock
            .Setup(x =>
                x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new Exception("Send failed"));

        Exception? caughtEx = null;
        try
        {
            await _busProvider.SendMessageAsync(request);
        }
        catch (Exception ex)
        {
            caughtEx = ex;
        }

        Assert.IsNotNull(caughtEx, "Expected an exception to be thrown.");
        Assert.AreEqual("Send failed", caughtEx.Message);

        _serviceBusSenderMock.VerifyAll();
    }
}
