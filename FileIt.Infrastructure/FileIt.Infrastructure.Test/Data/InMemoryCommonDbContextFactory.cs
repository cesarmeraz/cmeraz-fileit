using FileIt.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Test.Data;

/// <summary>
/// Shared test infrastructure for repo tests. Provides a fresh in-memory
/// CommonDbContext per test via a factory that mimics the runtime
/// IDbContextFactory contract.
/// </summary>
public sealed class InMemoryCommonDbContextFactory : IDbContextFactory<CommonDbContext>, IDisposable
{
    private readonly DbContextOptions<CommonDbContext> _options;

    public InMemoryCommonDbContextFactory(string? databaseName = null)
    {
        var dbName = databaseName ?? Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<CommonDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    public CommonDbContext CreateDbContext()
    {
        return new CommonDbContext(_options);
    }

    public Task<CommonDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateDbContext());
    }

    public void Dispose()
    {
        using var ctx = CreateDbContext();
        ctx.Database.EnsureDeleted();
    }
}
