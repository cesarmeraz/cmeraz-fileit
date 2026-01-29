using System.Configuration;
using FileIt.Common.App;
using FileIt.Common.App.ApiAdd;
using FileIt.Domain.Interfaces;
using FileIt.Domain.Logging;
using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Tools;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.UseMiddleware<MiddlewareLogger>();

builder
    .Services.AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var sectionName = builder.Configuration.GetValue<string>("FeatureSection") ?? "Feature";
CommonConfig? config = builder.Configuration.GetSection(sectionName).Get<CommonConfig>();
if (config == null)
{
    throw new ApplicationException("Appsettings.json is missing Feature config.");
}

builder.Services.AddSingleton(config);
builder.Services.AddScoped<IApiAddCommand, ApiAddCommand>();
builder.Services.AddScoped<IBroadcastResponses, PublishTool>();

IInfrastructureConfig infrastructureConfig = builder.GetInfrastructureConfig();
builder.Services.AddInfrastructure(infrastructureConfig);

// Configure logging
builder.Logging.ClearProviders(); // Remove default logging providers
ICommonLogConfig logConfig = builder.GetCommonLogConfig();
logConfig.FeatureVersion = System
    .Reflection.Assembly.GetExecutingAssembly()
    .GetName()
    .Version?.ToString();
builder.Logging.AddCommonLog(logConfig);

builder.Build().Run();
