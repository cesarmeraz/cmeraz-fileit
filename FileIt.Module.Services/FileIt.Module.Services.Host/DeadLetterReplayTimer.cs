// Timer-triggered batch replay function. Drains the PendingReplay backlog by
// calling IDeadLetterReplayService.ReplayBatchAsync at a regular cadence.
//
// All replay logic lives in the service. This function is a thin schedule adapter:
// it owns the trigger, names the initiator, and bounds the batch size.
//
// See docs/dead-letter-strategy.md for the full design.
using FileIt.Infrastructure;
using FileIt.Infrastructure.DeadLetter.Replay;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Services.Host;

/// <summary>
/// Azure Function that runs on a timer schedule and processes any
/// <see cref="FileIt.Domain.Entities.DeadLetter.DeadLetterRecordStatus.PendingReplay"/>
/// records the operator (or another scheduled job) has promoted.
/// </summary>
/// <remarks>
/// <para>
/// Schedule defaults to every five minutes. Five minutes is short enough that an
/// operator-promoted record is processed within a coffee break, and long enough
/// that the batch path does not thrash the database when there is no work.
/// Override via the <c>DeadLetterReplaySchedule</c> app-setting if a different
/// cadence is needed in a given environment.
/// </para>
/// <para>
/// Batch size is capped at 25 per tick to bound the worst-case duration of a
/// single invocation and to give the Functions runtime visibility into progress
/// (rather than appearing hung for minutes if a backlog of thousands accumulates).
/// </para>
/// </remarks>
public class DeadLetterReplayTimer
{
    public const string FunctionName = nameof(DeadLetterReplayTimer);

    /// <summary>
    /// CRON expression for the timer trigger. Five-minute cadence by default; the
    /// <c>%DeadLetterReplaySchedule%</c> placeholder lets a deployment override
    /// the schedule via app-settings without recompiling.
    /// </summary>
    public const string Schedule = "%DeadLetterReplaySchedule%";

    /// <summary>
    /// Hard ceiling on records processed per tick. See class remarks for rationale.
    /// </summary>
    public const int BatchSize = 25;

    private readonly IDeadLetterReplayService _replay;
    private readonly ILogger<DeadLetterReplayTimer> _logger;

    public DeadLetterReplayTimer(
        IDeadLetterReplayService replay,
        ILogger<DeadLetterReplayTimer> logger)
    {
        _replay = replay ?? throw new ArgumentNullException(nameof(replay));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [Function(FunctionName)]
    public async Task Run(
        [TimerTrigger(Schedule)] TimerInfo timerInfo,
        FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(timerInfo);
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation(
            InfrastructureEvents.ReplayFunctionStarted,
            "{FunctionName} tick: scheduled status={ScheduleStatus}.",
            FunctionName,
            timerInfo.ScheduleStatus is null
                ? "<no-schedule-status>"
                : $"Last={timerInfo.ScheduleStatus.Last:O}, "
                    + $"Next={timerInfo.ScheduleStatus.Next:O}");

        var outcomes = await _replay.ReplayBatchAsync(
                maxRecords: BatchSize,
                initiatedBy: FunctionName,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation(
            InfrastructureEvents.ReplayFunctionStopped,
            "{FunctionName} tick complete: {OutcomeCount} outcomes processed.",
            FunctionName,
            outcomes.Count);
    }
}