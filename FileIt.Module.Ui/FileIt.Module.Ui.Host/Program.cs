using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.Middleware;
using FileIt.Module.Ui.App;
using FileIt.Module.Ui.App.WaitOnApiUpload;
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


var sectionName = builder.Configuration.GetValue<string>("FeatureSection") ?? "Feature";
UiConfig? config = builder.Configuration.GetSection(sectionName).Get<UiConfig>();
if (config == null)
{
    throw new ApplicationException("appsettings.json is missing Feature config.");
}

builder.Services.AddSingleton(config);
builder.Services.AddScoped<IWatchInbound, WatchInbound>();
builder.Services.AddScoped<IBasicApiAddHandler, BasicApiAddHandler>();

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
