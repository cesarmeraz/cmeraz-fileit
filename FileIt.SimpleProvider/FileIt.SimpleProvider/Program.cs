using System.Configuration;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Tools;
using FileIt.SimpleProvider.App;
using FileIt.SimpleProvider.App.WaitOnApiUpload;
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

var programTool = new ProgramTool();
SimpleConfig? featureConfig = programTool.GetFeatureConfig<SimpleConfig>(builder);
if (featureConfig == null)
    throw new ConfigurationErrorsException(
        "Error parsing SimpleConfig. Possible cause: appSettings.json is missing a Feature section."
    );
featureConfig.FeatureVersion = System
    .Reflection.Assembly.GetExecutingAssembly()
    .GetName()
    .Version?.ToString();

builder.Services.AddSingleton(featureConfig);
builder.Services.AddSingleton<IFeatureConfig>(featureConfig);
builder.Services.AddInfrastructure(featureConfig);
builder.Services.AddScoped<IWatchInbound, WatchInbound>();
builder.Services.AddScoped<IBasicApiAddHandler, BasicApiAddHandler>();

// Configure logging
builder.Logging.ClearProviders(); // Remove default logging providers
builder.Logging.AddCommonLogger(featureConfig);

builder.Build().Run();
