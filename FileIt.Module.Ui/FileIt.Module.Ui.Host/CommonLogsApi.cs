using FileIt.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Ui.Host;

public class CommonLogsApi
{
    private readonly IDbContextFactory<CommonDbContext> _factory;
    private readonly ILogger<CommonLogsApi> _logger;

    public CommonLogsApi(IDbContextFactory<CommonDbContext> factory, ILogger<CommonLogsApi> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    [Function("GetCommonLogs")]
    public async Task<IActionResult> GetCommonLogs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/common-logs")] HttpRequest req,
        CancellationToken token)
    {

        await using var db = _factory.CreateDbContext();
        var logs = await db.CommonLogs
            .AsNoTracking()
            .Where(x => x.Application != "FileIt.Module.Services.Host")
            .Take(1000) // for testing
            .Select(x => new
            {
                x.Id,
                x.Application,
                x.InvocationId,
                x.EventName,
                x.CreatedOn
            })
            .ToListAsync();

        return new OkObjectResult(logs);
    }
}

