using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data;

public class ApiLogRepo : BaseRepository<ApiLog>, IApiLogRepo
{
    public ApiLogRepo(IDbContextFactory<CommonDbContext> dbContextFactory)
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
