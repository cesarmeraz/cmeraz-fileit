using FileIt.SqlProvider.Features.Simple;
using Microsoft.EntityFrameworkCore;

namespace FileIt.SqlProvider.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<SimpleRequestLog> SimpleRequestLogs { get; set;} 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder
                .Entity<SimpleRequestLog>()
                .ToTable("SimpleRequestLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
