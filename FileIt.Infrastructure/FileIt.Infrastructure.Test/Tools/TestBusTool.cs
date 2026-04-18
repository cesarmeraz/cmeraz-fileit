using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Infrastructure.Tools;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.Abstractions;
using TUnit.Assertions;
using TUnit.Core;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace FileIt.Infrastructure.Test.Tools;

public class TestBusTool
{
    private readonly Mock<ServiceBusSender> _serviceBusSenderMock;
    private readonly Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
    private readonly BusTool _busProvider;

    public TestBusTool()
    {
        var repository = new MockRepository();

        _serviceBusSenderMock = repository.Of<ServiceBusSender>();
        _senderFactoryMock = repository.Of<IAzureClientFactory<ServiceBusSender>>();

        _senderFactoryMock.CreateClient(Arg.Any<string>()).Returns(_serviceBusSenderMock.Object);

        _busProvider = new BusTool(NullLogger<BusTool>.Instance, _senderFactoryMock.Object);
    }

    [Test]
    public async Task SendMessageAsync_ShouldSendMessage_WhenCalled()
    {
        var messageId = "test-message-id";
        var message = new ServiceBusMessage("Test message");
        var request = new ApiRequest(messageId) { Body = "Test message", QueueName = "testqueue" };

        _serviceBusSenderMock
            .SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .Returns();

        await _busProvider.SendMessageAsync(request);

        _serviceBusSenderMock.VerifyAll();
    }

    [Test]
    public async Task SendMessageAsync_ShouldThrow_WhenSendFails()
    {
        var messageId = "test-message-id";
        var message = new ServiceBusMessage("Test message");
        var request = new ApiRequest(messageId) { Body = "Test message", QueueName = "testqueue" };

        _serviceBusSenderMock
            .SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>())
            .Throws(new System.Exception("Send failed"));

        await Assert.ThrowsAsync<System.Exception>(() => _busProvider.SendMessageAsync(request));

        _serviceBusSenderMock.VerifyAll();
    }
}
