using FileIt.App.Models;
using Microsoft.EntityFrameworkCore;

namespace FileIt.App.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<SimpleRequestLog> SimpleRequestLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SimpleRequestLog>().HasKey(p => p.Id);
            // Configure entity mappings, keys, relationships, indexes, etc.
        }
    }
}
