using FileIt.App.Api;
using FileIt.App.Models;
using FileIt.App.Simple;
using Microsoft.EntityFrameworkCore;

namespace FileIt.App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<SimpleRequestLog> SimpleRequestLogs { get; set; }
        public DbSet<ApiLog> ApiLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder
                .Entity<ApiLog>()
                .ToTable("ApiLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
            modelBuilder
                .Entity<SimpleRequestLog>()
                .ToTable("SimpleRequestLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
