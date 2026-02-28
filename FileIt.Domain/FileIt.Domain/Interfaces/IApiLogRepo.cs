using FileIt.Domain.Entities.Api;

namespace FileIt.Domain.Interfaces;

public interface IApiLogRepo : IRepository<ApiLog>
{
    Task<ApiLog?> AddAsync(
        string clientRequestId,
        string requestBody,
        string responseBody,
        string status
    );
    Task<ApiLog?> GetByClientRequestIdAsync(string clientRequestId);
}
