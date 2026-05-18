using Microsoft.Extensions.Logging;

namespace FileIt.Module.Complex.App.Behavior;

public interface IChaosInjector
{
    /// <summary>
    /// True if this request should be rejected with a synthetic 503.
    /// Caller is responsible for the actual response.
    /// </summary>
    bool ShouldFail(string requestPath);

    int RetryAfterSeconds { get; }
}

public class ChaosInjector : IChaosInjector
{
    private readonly ComplexConfig _config;
    private readonly ILogger<ChaosInjector> _logger;
    private readonly Random _rng = new();
    private readonly object _rngLock = new();

    public ChaosInjector(ComplexConfig config, ILogger<ChaosInjector> logger)
    {
        _config = config;
        _logger = logger;
    }

    public int RetryAfterSeconds => _config.Chaos.RetryAfterSeconds;

    public bool ShouldFail(string requestPath)
    {
        if (!_config.Chaos.Enabled)
        {
            return false;
        }

        if (_config.Chaos.Failure503Rate <= 0.0)
        {
            return false;
        }

        // Exempt configured paths so health checks and swagger never flake.
        foreach (var exempt in _config.Chaos.ExemptPaths ?? Array.Empty<string>())
        {
            if (requestPath.StartsWith(exempt, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        double roll;
        lock (_rngLock)
        {
            roll = _rng.NextDouble();
        }

        var fail = roll < _config.Chaos.Failure503Rate;
        if (fail)
        {
            _logger.LogWarning(ComplexEvents.ChaosFailureInjected,
                "Chaos: synthetic 503 for {Path} (roll={Roll:F4}, threshold={Threshold:F4})",
                requestPath, roll, _config.Chaos.Failure503Rate);
        }
        return fail;
    }
}
