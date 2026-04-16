// This is the contract for our DataFlow database repository.
// It defines what operations we need to track a GL Account file
// as it moves through the flow.
// The actual implementation lives in the Infrastructure project.
using FileIt.Domain.Entities;

namespace FileIt.Domain.Interfaces;

public interface IDataFlowRequestLogRepo : IRepository<DataFlowRequestLog>
{
    // Creates a new log entry when a CSV file first arrives
    Task<DataFlowRequestLog?> AddAsync(string blobName, string clientRequestId);

    // Looks up a log entry by the correlation ID we assigned when the file arrived
    Task<DataFlowRequestLog?> GetByClientRequestIdAsync(string? clientRequestId);
}
