using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.Middleware;
using FileIt.Module.FileItModule.App;
//#if (IncludeSubscriber)
using FileIt.Module.FileItModule.App.WaitOnApiUpload;
//#endif
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.UseMiddleware<MiddlewareLogger>();
builder.UseMiddleware<SerilogInvocationIdMiddleware>();
builder.UseMiddleware<ExceptionHandlingMiddleware>();

#if RELEASE
// Add Application Insights telemetry if deployed to Azure
builder.Services.AddApplicationInsightsTelemetryWorkerService();
#endif

var sectionName = builder.Configuration.GetValue<string>("FeatureSection") ?? "Feature";
FileItModuleConfig? config = builder.Configuration.GetSection(sectionName).Get<FileItModuleConfig>();
if (config == null)
{
    throw new ApplicationException("appsettings.json is missing Feature config.");
}

builder.Services.AddSingleton(config);
//#if (IncludeWatcher)
builder.Services.AddScoped<IWatchInbound, WatchInbound>();
//#endif
//#if (IncludeSubscriber)
builder.Services.AddScoped<IBasicApiAddHandler, BasicApiAddHandler>();
//#endif

var infrastructureConfig = builder.GetInfrastructureConfig();
builder.Services.AddInfrastructure(infrastructureConfig);

// Configure logging
builder.Logging.ClearProviders();
ICommonLogConfig logConfig = builder.Configuration.GetCommonLogConfig();
logConfig.Environment = logConfig.Environment ?? builder.Environment.EnvironmentName;
logConfig.Application = builder.Environment.ApplicationName;
logConfig.ApplicationVersion = System
    .Reflection.Assembly.GetExecutingAssembly()
    .GetName()
    .Version?.ToString();
builder.Services.AddSingleton(logConfig);
builder.Logging.AddCommonLog(logConfig);

builder.Build().Run();
