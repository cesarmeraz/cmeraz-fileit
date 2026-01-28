using System.Configuration;
using FileIt.Common.App;
using FileIt.Common.App.ApiAdd;
using FileIt.Domain.Interfaces;
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

// IConfigurationRoot config = builder
//     .Configuration.SetBasePath(Directory.GetCurrentDirectory())
//     .AddJsonFile("appsettings.json")
//     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
//     .AddEnvironmentVariables()
//     .AddJsonFile("local.settings.json", true, false)
//     .Build();

var programTool = new ProgramTool();
CommonConfig? featureConfig = programTool.GetFeatureConfig<CommonConfig>(builder);
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
builder.Services.AddScoped<IApiAddCommand, ApiAddCommand>();
builder.Services.AddScoped<IBroadcastResponses, PublishTool>();

// Configure logging
builder.Logging.ClearProviders(); // Remove default logging providers
builder.Logging.AddCommonLogger(featureConfig);

builder.Build().Run();
