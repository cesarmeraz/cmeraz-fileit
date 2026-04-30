using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Behavior;

public interface ILatencyInjector
{
    Task DelayAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Injects a uniformly-random delay between MinMs and MaxMs (inclusive)
/// before returning a response. Disabled by config.
/// </summary>
public class LatencyInjector : ILatencyInjector
{
    private readonly ComplexConfig _config;
    private readonly ILogger<LatencyInjector> _logger;
    private readonly Random _rng = new();
    private readonly object _rngLock = new();

    public LatencyInjector(ComplexConfig config, ILogger<LatencyInjector> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task DelayAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Latency.Enabled)
        {
            return;
        }

        var min = Math.Max(0, _config.Latency.MinMs);
        var max = Math.Max(min, _config.Latency.MaxMs);

        int delayMs;
        lock (_rngLock)
        {
            delayMs = _rng.Next(min, max + 1);
        }

        if (delayMs > 0)
        {
            _logger.LogDebug(ComplexEvents.LatencyInjected,
                "Latency injection: sleeping {DelayMs}ms", delayMs);
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
        }
    }
}
