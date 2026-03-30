using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.Middleware;
using FileIt.Infrastructure.Tools;
using FileIt.SimpleFlow.App;
using FileIt.SimpleFlow.App.WaitOnApiUpload;
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

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var sectionName = builder.Configuration.GetValue<string>("FeatureSection") ?? "Feature";
SimpleConfig? config = builder.Configuration.GetSection(sectionName).Get<SimpleConfig>();
if (config == null)
{
    throw new ApplicationException("Appsettings.json is missing Feature config.");
}

builder.Services.AddSingleton(config);
builder.Services.AddScoped<IWatchInbound, WatchInbound>();
builder.Services.AddScoped<IBasicApiAddHandler, BasicApiAddHandler>();

var infrastructureConfig = builder.GetInfrastructureConfig();
builder.Services.AddInfrastructure(infrastructureConfig);

// Configure logging
builder.Logging.ClearProviders(); // Remove default logging providers
ICommonLogConfig logConfig = builder.GetCommonLogConfig();
logConfig.ApplicationVersion = System
    .Reflection.Assembly.GetExecutingAssembly()
    .GetName()
    .Version?.ToString();
builder.Logging.AddCommonLog(logConfig);

builder.Build().Run();
