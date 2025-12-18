using FileIt.SqlProvider.Common.Tools;
using FileIt.SqlProvider.Data;
using Microsoft.EntityFrameworkCore;

namespace FileIt.SqlProvider.Features.Simple
{
    public interface ISimpleRequestLogRepo : IRepository<SimpleRequestLog>
    {
        Task<SimpleRequestLog?> AddAsync(string blobName, string clientRequestId);
        Task<SimpleRequestLog?> GetByClientRequestIdAsync(string? clientRequestId);
    }

    public class SimpleRequestLogRepo : BaseRepository<SimpleRequestLog>, ISimpleRequestLogRepo
    {
        public SimpleRequestLogRepo(
            IDbContextFactory<AppDbContext> dbContextFactory,
            CommonConfig appConfig
        )
            : base(dbContextFactory, appConfig) { }

        public async Task<SimpleRequestLog?> AddAsync(string blobName, string clientRequestId)
        {
            var log = new SimpleRequestLog
            {
                Environment = this.appConfig.Environment,
                Host = this.appConfig.Host,
                Agent = this.appConfig.Agent,
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
                log.ClientRequestId == clientRequestId
                && log.Environment == this.appConfig.Environment
            );
        }
    }
}
