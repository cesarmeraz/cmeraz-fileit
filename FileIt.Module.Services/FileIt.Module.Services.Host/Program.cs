using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.Middleware;
using FileIt.Module.Services.App;
using FileIt.Module.Services.App.ApiAdd;
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
ServicesConfig? config = builder.Configuration.GetSection(sectionName).Get<ServicesConfig>();
if (config == null)
{
    throw new ApplicationException("Appsettings.json is missing Feature config.");
}

builder.Services.AddSingleton(config);
builder.Services.AddScoped<IApiAddCommand, ApiAddCommand>();

var infrastructureConfig = builder.GetInfrastructureConfig();
builder.Services.AddInfrastructure(infrastructureConfig);
builder.Services.AddIBroadcastResponses();

// Configure logging
builder.Logging.ClearProviders(); // Remove default logging providers
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
