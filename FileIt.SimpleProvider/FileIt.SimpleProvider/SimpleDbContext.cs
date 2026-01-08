using Microsoft.EntityFrameworkCore;

namespace FileIt.SimpleProvider
{
    public class SimpleDbContext : DbContext
    {
        public SimpleDbContext(DbContextOptions<SimpleDbContext> options)
            : base(options) { }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<SimpleRequestLog> SimpleRequestLogs { get; set; }

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
