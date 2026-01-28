using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data
{
    public class CommonDbContext : DbContext
    {
        public CommonDbContext(DbContextOptions options)
            : base(options) { }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<ApiLog> ApiLogs { get; set; }
        public DbSet<CommonLog> CommonLogs { get; set; }
        public DbSet<SimpleRequestLog> SimpleRequestLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder
                .Entity<ApiLog>()
                .ToTable("ApiLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();

            modelBuilder
                .Entity<CommonLog>()
                .ToTable("CommonLog")
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
