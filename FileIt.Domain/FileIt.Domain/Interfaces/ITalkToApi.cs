using FileIt.Domain.Entities.Api;

namespace FileIt.Domain.Interfaces;

public interface ITalkToApi
{
    Task SendMessageAsync(ApiRequest message);
}
