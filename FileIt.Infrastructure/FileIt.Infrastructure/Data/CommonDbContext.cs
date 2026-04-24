using FileIt.Domain.Entities;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Entities.DeadLetter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FileIt.Infrastructure.Data
{
    public class CommonDbContext : DbContext
    {
        public CommonDbContext(DbContextOptions options)
            : base(options) { }

        public DbSet<ApiLog> ApiLogs { get; set; }
        public DbSet<CommonLog> CommonLogs { get; set; }
        public DbSet<SimpleRequestLog> SimpleRequestLogs { get; set; }

        // Our DataFlow table. Tracks GL Account CSV files through the pipeline.
        public DbSet<DataFlowRequestLog> DataFlowRequestLogs { get; set; }

        // Dead-letter records. Written by DLQ reader functions, read and updated by
        // operators and the replay function. See docs/dead-letter-strategy.md.
        public DbSet<DeadLetterRecord> DeadLetterRecords { get; set; }

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

            // Map our new entity to the DataFlowRequestLog table in SQL.
            modelBuilder
                .Entity<DataFlowRequestLog>()
                .ToTable("DataFlowRequestLog")
                .Property(s => s.Id)
                .ValueGeneratedOnAdd();

            ConfigureDeadLetterRecord(modelBuilder);
        }

        /// <summary>
        /// Maps <see cref="DeadLetterRecord"/> to <c>dbo.DeadLetterRecord</c>.
        /// Configures enum-to-string value converters so the C# type system and the
        /// CHECK constraints defined on the table stay in agreement: C# code cannot
        /// write an invalid enum value, and the database cannot accept a string
        /// outside the constrained set.
        /// </summary>
        private static void ConfigureDeadLetterRecord(ModelBuilder modelBuilder)
        {
            // Value converters. EnumToStringConverter<T> writes the enum name, reads it
            // back. Combined with the CHECK constraints on the table, drift between
            // code and database is impossible: a rename in C# produces a compile error
            // at the call site, and a rename in SQL produces a runtime parse error on
            // the first read.
            var failureCategoryConverter =
                new EnumToStringConverter<FailureCategory>();
            var statusConverter =
                new EnumToStringConverter<DeadLetterRecordStatus>();
            var sourceEntityTypeConverter =
                new EnumToStringConverter<SourceEntityType>();

            var entity = modelBuilder.Entity<DeadLetterRecord>();

            entity.ToTable("DeadLetterRecord");

            entity
                .Property(e => e.DeadLetterRecordId)
                .ValueGeneratedOnAdd();

            entity
                .Property(e => e.FailureCategory)
                .HasConversion(failureCategoryConverter)
                .HasMaxLength(32)
                .IsRequired();

            entity
                .Property(e => e.Status)
                .HasColumnName("Status")
                .HasConversion(statusConverter)
                .HasMaxLength(32)
                .IsRequired();

            entity
                .Property(e => e.SourceEntityType)
                .HasConversion(sourceEntityTypeConverter)
                .HasMaxLength(16)
                .IsRequired();

            // Max-length hints on the other string columns so EF generates correctly
            // sized parameters. Not required for correctness (the server would truncate
            // or reject on its own) but produces cleaner execution plans and matches
            // the declared column widths.
            entity.Property(e => e.MessageId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.SessionId).HasMaxLength(128);
            entity.Property(e => e.SourceEntityName).HasMaxLength(260).IsRequired();
            entity.Property(e => e.SourceSubscriptionName).HasMaxLength(260);
            entity.Property(e => e.DeadLetterReason).HasMaxLength(260);
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.StatusUpdatedBy).HasMaxLength(128);
            entity.Property(e => e.LastReplayMessageId).HasMaxLength(128);

            entity.Property(e => e.MessageBody).IsRequired();
        }
    }
}
