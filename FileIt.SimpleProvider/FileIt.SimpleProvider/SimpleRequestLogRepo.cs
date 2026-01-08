using FileIt.Common.Data;
using FileIt.Common.Tools;
using Microsoft.EntityFrameworkCore;

namespace FileIt.SimpleProvider
{
    public interface ISimpleRequestLogRepo : IRepository<SimpleRequestLog, SimpleDbContext>
    {
        Task<SimpleRequestLog?> AddAsync(string blobName, string clientRequestId);
        Task<SimpleRequestLog?> GetByClientRequestIdAsync(string? clientRequestId);
    }

    public class SimpleRequestLogRepo
        : BaseRepository<SimpleRequestLog, SimpleDbContext>,
            ISimpleRequestLogRepo
    {
        private readonly SimpleConfig config;

        public SimpleRequestLogRepo(
            IDbContextFactory<SimpleDbContext> dbContextFactory,
            SimpleConfig config
        )
            : base(dbContextFactory)
        {
            this.config = config;
        }

        public async Task<SimpleRequestLog?> AddAsync(string blobName, string clientRequestId)
        {
            var log = new SimpleRequestLog
            {
                Environment = this.config.Environment,
                Host = this.config.Host,
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
}
