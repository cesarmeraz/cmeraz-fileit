using FileIt.Common.Data;
using Microsoft.EntityFrameworkCore;

namespace FileIt.App
{
    public interface IApiLogRepo : IRepository<ApiLog, AppDbContext>
    {
        Task<ApiLog?> AddAsync(
            string clientRequestId,
            string requestBody,
            string responseBody,
            string status
        );
        Task<ApiLog?> GetByClientRequestIdAsync(string clientRequestId);
    }

    public class ApiLogRepo : BaseRepository<ApiLog, AppDbContext>, IApiLogRepo
    {
        public ApiLogRepo(IDbContextFactory<AppDbContext> dbContextFactory)
            : base(dbContextFactory) { }

        public async Task<ApiLog?> AddAsync(
            string clientRequestId,
            string requestBody,
            string responseBody,
            string status
        )
        {
            var log = new ApiLog()
            {
                ClientRequestId = clientRequestId,
                RequestBody = requestBody,
                ResponseBody = responseBody,
                Status = status,
            };
            return await this.AddAsync(log);
        }

        public async Task<ApiLog?> GetByClientRequestIdAsync(string clientRequestId)
        {
            using var dbContext = Factory.CreateDbContext();
            return await dbContext.ApiLogs.FirstOrDefaultAsync(log =>
                log.ClientRequestId == clientRequestId
            );
        }
    }
}
