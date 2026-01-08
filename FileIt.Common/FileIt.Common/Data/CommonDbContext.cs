using FileIt.Common.Domain;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Common.Data
{
    public class CommonDbContext : DbContext
    {
        private readonly string _connectionString;

        public CommonDbContext(string connectionString)
            : base()
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        // Register your DbSet<TEntity> properties here, for example:
        public DbSet<CommonLog> CommonLogs { get; set; }

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
