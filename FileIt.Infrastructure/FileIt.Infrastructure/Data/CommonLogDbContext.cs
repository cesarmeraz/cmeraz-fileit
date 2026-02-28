using FileIt.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data
{
    public class CommonLogDbContext : DbContext
    {
        private readonly string _connectionString;

        public CommonLogDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<CommonLog> CommonLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder
                .Entity<CommonLog>()
                .ToTable("CommonLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
