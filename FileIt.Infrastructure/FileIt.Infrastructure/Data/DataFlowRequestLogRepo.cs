// This is the concrete implementation of our DataFlow database repository.
// It handles reading and writing DataFlowRequestLog records to SQL.
// The Domain project only knows about the interface — this is where
// the actual Entity Framework database calls live.
using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data;

public class DataFlowRequestLogRepo : BaseRepository<DataFlowRequestLog>, IDataFlowRequestLogRepo
{
    private readonly ICommonLogConfig config;

    public DataFlowRequestLogRepo(
        IDbContextFactory<CommonDbContext> dbContextFactory,
        ICommonLogConfig config
    )
        : base(dbContextFactory)
    {
        this.config = config;
    }

    // Creates a new log entry when a CSV file first arrives in the source container
    public async Task<DataFlowRequestLog?> AddAsync(string blobName, string clientRequestId)
    {
        var log = new DataFlowRequestLog
        {
            Environment = this.config.Environment ?? string.Empty,
            Host = this.config.Host ?? string.Empty,
            Agent = this.config.Agent,
            BlobName = blobName,
            ClientRequestId = clientRequestId,
            CreatedOn = DateTime.Now,
            ModifiedOn = DateTime.Now,
            RowsIngested = 0,
            RowsTransformed = 0,
            Status = "New",
            Comment = null,
        };
        return await base.AddAsync(log);
    }

    // Looks up a log entry by correlation ID — used when the transform completes
    // to find the original record and update it with the results
    public async Task<DataFlowRequestLog?> GetByClientRequestIdAsync(string? clientRequestId)
    {
        using var dbContext = Factory.CreateDbContext();
        return await dbContext.DataFlowRequestLogs.FirstOrDefaultAsync(log =>
            log.ClientRequestId == clientRequestId && log.Environment == this.config.Environment
        );
    }
}
