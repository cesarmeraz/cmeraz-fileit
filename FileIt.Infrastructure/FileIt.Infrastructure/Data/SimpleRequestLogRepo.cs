using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using FileIt.Domain.Logging;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data;

public class SimpleRequestLogRepo : BaseRepository<SimpleRequestLog>, ISimpleRequestLogRepo
{
    private readonly ICommonLogConfig config;

    public SimpleRequestLogRepo(
        IDbContextFactory<CommonDbContext> dbContextFactory,
        ICommonLogConfig config
    )
        : base(dbContextFactory)
    {
        this.config = config;
    }

    public async Task<SimpleRequestLog?> AddAsync(string blobName, string clientRequestId)
    {
        var log = new SimpleRequestLog
        {
            Environment = this.config.Environment ?? string.Empty,
            Host = this.config.Host ?? string.Empty,
            Agent = this.config.Agent,
            BlobName = blobName,
            ClientRequestId = clientRequestId,
            CreatedOn = DateTime.Now,
            ModifiedOn = DateTime.Now,
            ApiId = 0,
            Status = "New",
            Comment = null,
        };
        return await base.AddAsync(log);
    }

    public async Task<SimpleRequestLog?> GetByClientRequestIdAsync(string? clientRequestId)
    {
        using var dbContext = Factory.CreateDbContext();
        return await dbContext.SimpleRequestLogs.FirstOrDefaultAsync(log =>
            log.ClientRequestId == clientRequestId && log.Environment == this.config.Environment
        );
    }
}
