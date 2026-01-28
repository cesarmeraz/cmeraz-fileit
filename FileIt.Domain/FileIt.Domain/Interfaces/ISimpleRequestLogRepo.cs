using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;

namespace FileIt.Domain.Interfaces;

public interface ISimpleRequestLogRepo : IRepository<SimpleRequestLog>
{
    Task<SimpleRequestLog?> AddAsync(string blobName, string clientRequestId);
    Task<SimpleRequestLog?> GetByClientRequestIdAsync(string? clientRequestId);
}
