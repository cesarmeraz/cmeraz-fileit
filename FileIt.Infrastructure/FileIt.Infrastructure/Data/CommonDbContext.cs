using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data
{
    public class CommonDbContext : DbContext
    {
        public CommonDbContext(DbContextOptions options)
            : base(options) { }

        public DbSet<ApiLog> ApiLogs { get; set; }
        public DbSet<CommonLog> CommonLogs { get; set; }
        public DbSet<SimpleRequestLog> SimpleRequestLogs { get; set; }

        // Our new DataFlow table — tracks GL Account CSV files through the pipeline
        public DbSet<DataFlowRequestLog> DataFlowRequestLogs { get; set; }

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

            // Map our new entity to the DataFlowRequestLog table in SQL
            modelBuilder
                .Entity<DataFlowRequestLog>()
                .ToTable("DataFlowRequestLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
