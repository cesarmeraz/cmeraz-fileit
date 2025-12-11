using FileIt.App.Api;
using FileIt.App.Data;
using FileIt.App.Models;
using FileIt.App.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FileIt.App.Repositories
{
    public interface IApiLogRepo : IRepository<ApiLog>
    {
        Task<ApiLog?> AddAsync(
            string clientRequestId,
            string requestBody,
            string responseBody,
            string status
        );
        Task<ApiLog?> GetByClientRequestIdAsync(string clientRequestId);
    }

    public class ApiLogRepo : BaseRepository<ApiLog>, IApiLogRepo
    {
        public ApiLogRepo(IDbContextFactory<AppDbContext> dbContextFactory, AppConfig appConfig)
            : base(dbContextFactory, appConfig) { }

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
