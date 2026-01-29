using Azure;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;

namespace FileIt.Domain.Interfaces;

public interface IBroadcastResponses
{
    Task EmitAsync(ApiAddResponse response);
}
