using FileIt.Domain.Entities;

namespace FileIt.Domain.Interfaces;

public interface ICommonLogRepo : IRepository<CommonLog>
{
    Task<CommonLog?> GetByClientRequestIdAsync(string? clientRequestId);
}
