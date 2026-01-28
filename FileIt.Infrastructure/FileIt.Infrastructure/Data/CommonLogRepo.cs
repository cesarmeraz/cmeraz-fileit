using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data
{
    public class CommonLogRepo : BaseRepository<CommonLog>, ICommonLogRepo
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
