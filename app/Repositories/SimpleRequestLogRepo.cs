using FileIt.App.Data;
using FileIt.App.Models;

namespace FileIt.App.Repositories
{
    public interface ISimpleRequestLogRepo : IRepository<SimpleRequestLog>
    {
        Task AddLogAsync(string blobName, string clientRequestId);
    }

    public class SimpleRequestLogRepo : BaseRepository<SimpleRequestLog>, ISimpleRequestLogRepo
    {
        public SimpleRequestLogRepo(AppDbContext dbContext, AppConfig appConfig)
            : base(dbContext, appConfig) { }

        public async Task AddLogAsync(string blobName, string clientRequestId)
        {
            var log = new SimpleRequestLog
            {
                Environment = this.appConfig.Environment,
                Host = this.appConfig.Host,
                Agent = this.appConfig.Agent,
                BlobName = blobName,
                Comment = clientRequestId,
                CreatedOn = DateTime.UtcNow,
            };

            dbContext.SimpleRequestLogs.Add(log);
            await dbContext.SaveChangesAsync();
        }
    }
}
