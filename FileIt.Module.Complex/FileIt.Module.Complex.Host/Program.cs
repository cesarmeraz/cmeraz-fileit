using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Extensions;
using FileIt.Infrastructure.Logging;
using FileIt.Infrastructure.Middleware;
using FileIt.Module.Complex.App;
using FileIt.Module.Complex.App.Behavior;
using FileIt.Module.Complex.App.Commands;
using FileIt.Module.Complex.App.Queries;
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
builder.Services.AddApplicationInsightsTelemetryWorkerService();
#endif

// Bind feature config
var sectionName = builder.Configuration.GetValue<string>("FeatureSection") ?? "Feature";
ComplexConfig? config = builder.Configuration.GetSection(sectionName).Get<ComplexConfig>();
if (config == null)
{
    throw new ApplicationException("appsettings.json is missing Feature config for Complex module.");
}
builder.Services.AddSingleton(config);

// Behaviors
builder.Services.AddSingleton<ILatencyInjector, LatencyInjector>();
builder.Services.AddSingleton<IChaosInjector, ChaosInjector>();
builder.Services.AddScoped<IIdempotencyManager, IdempotencyManager>();

// Commands
builder.Services.AddScoped<ICreateDocumentCommand, CreateDocumentCommand>();
builder.Services.AddScoped<IDeleteDocumentCommand, DeleteDocumentCommand>();

// Queries
builder.Services.AddScoped<IGetDocumentQuery, GetDocumentQuery>();
builder.Services.AddScoped<IListDocumentsQuery, ListDocumentsQuery>();
builder.Services.AddScoped<IExportDocumentsQuery, ExportDocumentsQuery>();

// Infrastructure (DbContext, repos, etc.)
var infrastructureConfig = builder.GetInfrastructureConfig();
builder.Services.AddInfrastructure(infrastructureConfig);

// Logging
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
