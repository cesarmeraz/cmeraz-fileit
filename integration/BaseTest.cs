using System.Configuration;
using FileIt.App.Common.Tools;
using FileIt.App.Data;
using FileIt.App.Features.Simple;
using FileIt.App.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Sinks.MSSqlServer;

namespace FileIt.Integration.Test
{
    public abstract class BaseTest
    {
        protected readonly IConfiguration _configuration;
        protected ServiceProvider ServiceProvider;

        public BaseTest()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"appsettings.json", false, false)
                .AddEnvironmentVariables()
                .Build();
            var services = new ServiceCollection();

            ConfigTool? appConfig = _configuration.GetRequiredSection("App").Get<ConfigTool>();
            if (appConfig == null)
            {
                throw new ConfigurationErrorsException("Configuration is missing or invalid.");
            }
            string connstring =
                _configuration.GetConnectionString("FileItDb")
                ?? throw new ConfigurationErrorsException("FileItDb Connection string is missing.");
            Console.WriteLine($"Using connection string: {connstring}");
            // Register services here
            services.AddSingleton(appConfig.Api);
            services.AddSingleton(appConfig.Common);
            services.AddSingleton(appConfig.Simple);
            services.AddSingleton<IApiLogRepo, ApiLogRepo>();
            services.AddSingleton<ISimpleRequestLogRepo, SimpleRequestLogRepo>();
            services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connstring));
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
