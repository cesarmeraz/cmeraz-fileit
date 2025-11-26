using Microsoft.EntityFrameworkCore;

namespace FileIt.App.DataAccess.Context
{
    public partial class AppContext : DbContext
    {
        public AppContext(): base()
        {
        }

        public AppContext(DbContextOptions<AppContext> options): base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");

            modelBuilder.Entity<FileIt.App.Models.Api>().ToTable("Api");
            modelBuilder.Entity<FileIt.App.Models.SimpleRequestLog>().ToTable("SimpleRequestLog");
            modelBuilder.Entity<FileIt.App.Models.SimpleAuditLog>().ToTable("SimpleAuditLog");
        }
    }
}