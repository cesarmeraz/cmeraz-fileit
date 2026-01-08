using FileIt.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace FileIt.App
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<ApiLog> ApiLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder
                .Entity<ApiLog>()
                .ToTable("ApiLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
