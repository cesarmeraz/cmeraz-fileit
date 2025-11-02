using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

string buildConfiguration =
#if DEBUG
    "Debug";
#else
    "Release";
#endif

Console.WriteLine(
    $"Invoking Program.cs on {AppDomain.CurrentDomain.FriendlyName} in {buildConfiguration} at {DateTime.Now.ToLongTimeString()}!"
);

// Create the Host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(
        (context, services) =>
        {
            // Register services here
            services.AddSingleton<IMyService, MyService>();
        }
    )
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

// Resolve and use a service
var myService = host.Services.GetRequiredService<IMyService>();
myService.Run();

await host.RunAsync();

// Example service interface and implementation
public interface IMyService
{
    void Run();
}

public class MyService : IMyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        // Initialization code here
        _logger = logger;
    }

    public void Run()
    {
        _logger.LogCritical("LogCritical...");
        _logger.LogError("LogError...");
        _logger.LogDebug("LogDebug...");
        _logger.LogInformation("LogInformation...");
        _logger.LogWarning("LogWarning...");
    }
}
