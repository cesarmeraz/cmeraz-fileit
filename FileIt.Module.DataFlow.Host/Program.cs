using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.Middleware;
using FileIt.Module.DataFlow.App;
using FileIt.Module.DataFlow.App.WatchInbound;
using FileIt.Module.DataFlow.App.Transform;
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

// Load our DataFlow config from appsettings
var sectionName = builder.Configuration.GetValue<string>("FeatureSection") ?? "Feature";
DataFlowConfig? config = builder.Configuration.GetSection(sectionName).Get<DataFlowConfig>();
if (config == null)
{
    throw new ApplicationException("Appsettings.json is missing Feature config.");
}

builder.Services.AddSingleton(config);

// Register our DataFlow handlers
builder.Services.AddScoped<IWatchInbound, WatchInbound>();
builder.Services.AddScoped<ITransformGlAccounts, TransformGlAccounts>();

// Wire up the shared infrastructure (blob, service bus, database)
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
