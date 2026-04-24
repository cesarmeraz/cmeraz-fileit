using FileIt.Domain.Entities.DeadLetter;
using FileIt.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileIt.Infrastructure.Data
{
    /// <summary>
    /// Implementation of <see cref="IDeadLetterRecordRepo"/> against
    /// <see cref="CommonDbContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Does NOT inherit from <c>BaseRepository&lt;T&gt;</c>. The generic base is built
    /// around the <c>IAuditable</c> + <c>IRepository&lt;T&gt;</c> contract (int PK,
    /// local-time timestamps, generic CRUD) that does not fit this entity. See
    /// <see cref="DeadLetterRecord"/> for the full rationale on why this table
    /// diverges from the shared pattern.
    /// </para>
    /// <para>
    /// Every method creates its own <c>DbContext</c> from the factory, matching the
    /// short-lived context convention used by <c>CommonLogRepo</c> and the other
    /// repos in this project.
    /// </para>
    /// </remarks>
    public class DeadLetterRecordRepo : IDeadLetterRecordRepo
    {
        private readonly IDbContextFactory<CommonDbContext> _factory;

        public DeadLetterRecordRepo(IDbContextFactory<CommonDbContext> dbContextFactory)
        {
            _factory = dbContextFactory
                ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<DeadLetterRecord> InsertAsync(
            DeadLetterRecord record,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(record);

            // CreatedUtc and StatusUpdatedUtc have database defaults (SYSUTCDATETIME()),
            // but we set them explicitly here so the persisted values are exactly what
            // the application intended, not whatever the database clock happened to
            // read at INSERT time (which can drift from the application clock by
            // seconds in some environments).
            var now = DateTime.UtcNow;
            if (record.CreatedUtc == default)
            {
                record.CreatedUtc = now;
            }
            if (record.StatusUpdatedUtc == default)
            {
                record.StatusUpdatedUtc = now;
            }

            using var dbContext = _factory.CreateDbContext();
            dbContext.DeadLetterRecords.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);
            return record;
        }

        public async Task<bool> ExistsAsync(
            string messageId,
            string sourceEntityName,
            DateTime deadLetteredTimeUtc,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(messageId);
            ArgumentException.ThrowIfNullOrEmpty(sourceEntityName);

            using var dbContext = _factory.CreateDbContext();
            return await dbContext.DeadLetterRecords
                .AsNoTracking()
                .AnyAsync(
                    r => r.MessageId == messageId
                        && r.SourceEntityName == sourceEntityName
                        && r.DeadLetteredTimeUtc == deadLetteredTimeUtc,
                    cancellationToken);
        }

        public async Task<DeadLetterRecord?> GetByIdAsync(
            long deadLetterRecordId,
            CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDbContext();
            return await dbContext.DeadLetterRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.DeadLetterRecordId == deadLetterRecordId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<DeadLetterRecord>> GetPendingReplayBatchAsync(
            int maxRecords,
            CancellationToken cancellationToken = default)
        {
            if (maxRecords <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRecords),
                    maxRecords,
                    "Batch size must be positive.");
            }

            using var dbContext = _factory.CreateDbContext();
            return await dbContext.DeadLetterRecords
                .AsNoTracking()
                .Where(r => r.Status == DeadLetterRecordStatus.PendingReplay)
                .OrderBy(r => r.StatusUpdatedUtc)
                .Take(maxRecords)
                .ToListAsync(cancellationToken);
        }

        public async Task UpdateLifecycleAsync(
            long deadLetterRecordId,
            DeadLetterRecordStatus status,
            string? statusUpdatedBy,
            int? replayAttemptCount = null,
            DateTime? lastReplayAttemptUtc = null,
            string? lastReplayMessageId = null,
            string? resolutionNotes = null,
            CancellationToken cancellationToken = default)
        {
            using var dbContext = _factory.CreateDbContext();

            var entity = await dbContext.DeadLetterRecords
                .FirstOrDefaultAsync(
                    r => r.DeadLetterRecordId == deadLetterRecordId,
                    cancellationToken);

            if (entity is null)
            {
                throw new InvalidOperationException(
                    $"DeadLetterRecord {deadLetterRecordId} not found for lifecycle update.");
            }

            entity.Status = status;
            entity.StatusUpdatedUtc = DateTime.UtcNow;
            entity.StatusUpdatedBy = statusUpdatedBy;

            if (replayAttemptCount.HasValue)
            {
                entity.ReplayAttemptCount = replayAttemptCount.Value;
            }
            if (lastReplayAttemptUtc.HasValue)
            {
                entity.LastReplayAttemptUtc = lastReplayAttemptUtc.Value;
            }
            if (lastReplayMessageId is not null)
            {
                entity.LastReplayMessageId = lastReplayMessageId;
            }
            if (resolutionNotes is not null)
            {
                entity.ResolutionNotes = resolutionNotes;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
