using System.Text;
using Azure.Storage.Blobs;
using FileIt.SimpleFlow.App;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.SimpleFlow;

public class HelloWorld
{
    private readonly SimpleConfig _config;
    private readonly ILogger<HelloWorld> _logger;

    public HelloWorld(ILogger<HelloWorld> logger, SimpleConfig config)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// A testing aid that uploads a file to the blob storage emulator
    /// </summary>
    /// <param name="req">the HttpRequestData</param>
    /// <param name="executionContext">the FunctionContext</param>
    /// <returns></returns>
    [Function(nameof(HelloWorld))]
    public async Task Run([TimerTrigger("%EveryMinuteSchedule%")] TimerInfo myTimer)
    {
        using (
            _logger!.BeginScope(
                new Dictionary<string, object>() { { "EventId", SimpleEvents.SimpleTest } }
            )
        )
        {
            _logger.LogInformation("Hello World");
        }
    }
}
