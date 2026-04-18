using System.Text;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using FileIt.Module.Services.App.ApiAdd;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging.Abstractions;

namespace FileIt.Module.Services.App.Test.ApiAdd;

public class TestApiFunc
{
    private readonly Mock<IApiLogRepo> _apiLogRepoMock;
    private readonly Mock<IAzureClientFactory<ServiceBusSender>> _senderFactoryMock;
    private readonly Mock<IBroadcastResponses> _broadcasterMock;
    private readonly ApiAddCommand target;

    public TestApiFunc()
    {
        var repository = new MockRepository();

        _apiLogRepoMock = repository.Of<IApiLogRepo>();
        _senderFactoryMock = repository.Of<IAzureClientFactory<ServiceBusSender>>();
        _broadcasterMock = repository.Of<IBroadcastResponses>();

        _broadcasterMock.EmitAsync(Arg.Any<ApiAddResponse>()).Returns();

        target = new ApiAddCommand(
            _apiLogRepoMock.Object,
            _senderFactoryMock.Object,
            NullLogger<ApiAddCommand>.Instance,
            _broadcasterMock.Object
        );
    }

    [Test]
    public async Task Test()
    {
        const string replyTo = "replyTo";
        const string subject = "api-add-simple";
        var clientRequestId = Guid.NewGuid().ToString();
        var messageBody = new BinaryData(Encoding.UTF8.GetBytes("This is a test message body."));

        _apiLogRepoMock
            .AddAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(new ApiLog() { Id = 1 });

        var messageId = "test-message-id";
        var mockMessage = new ApiRequest(messageId)
        {
            Body = messageBody.ToString(),
            ReplyTo = replyTo,
            Subject = subject,
            CorrelationId = clientRequestId,
        };

        await target.ApiAdd(mockMessage);

        _apiLogRepoMock.VerifyAll();
        _broadcasterMock.VerifyAll();
    }
}
