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

    // TODO: Rename API route
    [Function("GetCommonLogs")]
    public async Task<IActionResult> GetCommonLogs(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/common-logs")] HttpRequest req,
        CancellationToken token)
    {
        // Collect optional params
        string? applicationName = req.Query["applicationName"];
        string? dateRange = req.Query["dateRange"];
        var now = DateTime.UtcNow;
        DateTime? startDate = dateRange switch
        {
            "1h" => now.AddHours(1),
            "24h" => now.AddHours(-24),
            "7d" => now.AddDays(-7),
            "30d" => now.AddDays(-30),
            _ => null
        };
        DateTime? endDate = startDate is not null ? now : null;

        // Query database
        await using var db = _factory.CreateDbContext();
        var commonLogs = await db.Database
            .SqlQuery<CommonLogSummaryDto>($"EXEC dbo.usp_GetCommonLogSummary @ApplicationName={applicationName},@StartDate={startDate},@EndDate={endDate}")
            .ToListAsync(token);

        return new OkObjectResult(commonLogs);
    }

    // TODO: Rename API route
    [Function("GetCommonLogDetails")]
    public async Task<IActionResult> GetCommonLogDetails(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/common-log-details/{invocationId}")] HttpRequest req,
        string invocationId,
        CancellationToken token)
    {
        await using var db = _factory.CreateDbContext();
        var commonLogs = await db.Database
            .SqlQuery<CommonLogDetailDto>($"EXEC dbo.usp_GetCommonLogDetail @InvocationId={invocationId}")
            .ToListAsync(token);

        return new OkObjectResult(commonLogs);
    }
}

