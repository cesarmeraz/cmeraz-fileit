using FileIt.Common.Data;
using FileIt.Common.Domain;
using FileIt.Common.Tools;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Common.Domain
{
    public interface ICommonLogRepo : IRepository<CommonLog, CommonDbContext>
    {
        Task<CommonLog?> GetByClientRequestIdAsync(string? clientRequestId);
    }

    public class CommonLogRepo : BaseRepository<CommonLog, CommonDbContext>, ICommonLogRepo
    {
        private readonly IFeatureConfig config;

        public CommonLogRepo(
            IDbContextFactory<CommonDbContext> dbContextFactory,
            IFeatureConfig config
        )
            : base(dbContextFactory)
        {
            this.config = config;
        }

        public async Task<CommonLog?> GetByClientRequestIdAsync(string? clientRequestId)
        {
            using var dbContext = Factory.CreateDbContext();
            return await dbContext.CommonLogs.FirstOrDefaultAsync(log =>
                log.CorrelationId == clientRequestId && log.Environment == this.config.Environment
            );
        }
    }
}
