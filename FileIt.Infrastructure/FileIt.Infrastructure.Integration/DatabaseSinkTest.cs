using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Serilog.Parsing;

namespace FileIt.Infrastructure.Integration;

[TestClass]
public class DatabaseSinkTest
{
    [TestMethod]
    public void TestEmit()
    {
        // Same configuration layering as TestHost so this test reads the same
        // FileItDbConnection key the rest of Infrastructure expects. The env var
        // FileItDbConnection (set at User scope) overrides the json values.
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("FileIt.Infrastructure.Integration.testconfig.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        string connString =
            configuration.GetValue<string>("FileItDbConnection")
            ?? throw new ConfigurationErrorsException(
                "FileItDbConnection is missing. Set the env var or add it to local config."
            );

        IConfigurationSection featureSection = configuration.GetRequiredSection("Feature");
        string environment = featureSection["Environment"]
            ?? throw new ConfigurationErrorsException("Feature:Environment is missing.");
        string host = featureSection["Host"]
            ?? throw new ConfigurationErrorsException("Feature:Host is missing.");
        string agent = featureSection["Agent"]
            ?? throw new ConfigurationErrorsException("Feature:Agent is missing.");
        string application = featureSection["Application"]
            ?? throw new ConfigurationErrorsException("Feature:Application is missing.");

        string correlationId = Guid.NewGuid().ToString();

        ICommonLogConfig featureConfig = new CommonLogConfig()
        {
            Agent = agent,
            Application = application,
            Environment = environment,
            Host = host,
            DbConnectionString = connString,
            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
        };
        var target = new DatabaseSink(featureConfig);

        // Create message template tokens
        var messageTemplate = new MessageTemplate(
            new MessageTemplateToken[]
            {
                new TextToken("Hello, "),
                new PropertyToken("Name", "{Name}"),
            }
        );
        var properties = new List<LogEventProperty>
        {
            new LogEventProperty("Name", new ScalarValue("World")),
            new LogEventProperty("EventId", new ScalarValue(1)),
            new LogEventProperty("CorrelationId", new ScalarValue(correlationId)),
            new LogEventProperty("SourceContext", new ScalarValue("SourceContext")),
        };

        // Manually instantiate the LogEvent
        var logEvent = new LogEvent(
            timestamp: DateTimeOffset.Now,
            level: LogEventLevel.Information,
            exception: null,
            messageTemplate: messageTemplate,
            properties: properties
        );

        //Act
        target.Emit(logEvent);
        List<CommonLog> actual = new List<CommonLog>();
        string sqlText = $"SELECT * FROM CommonLog where CorrelationId = '{correlationId}';";
        Console.WriteLine($"sqlText: {sqlText}");
        using (var connection = new SqlConnection(connString))
        {
            using (var command = new SqlCommand(sqlText, connection))
            {
                try
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Column names match the CommonLog entity property names
                            // because no [Column(...)] attributes remap them. The
                            // previous version asked for "Feature"/"FeatureVersion"
                            // which do not exist; correct columns are "Application"
                            // and "ApplicationVersion" (bug 7.7).
                            var log = new CommonLog()
                            {
                                CorrelationId = reader["CorrelationId"].ToString(),
                                Environment = reader["Environment"].ToString(),
                                Application = reader["Application"].ToString(),
                                Level = reader["Level"].ToString(),
                                MachineName = reader["MachineName"].ToString(),
                                SourceContext = reader["SourceContext"].ToString(),
                                Message = reader["Message"].ToString(),
                                ApplicationVersion = reader["ApplicationVersion"].ToString(),
                            };
                            actual.Add(log);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }

                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual(correlationId, actual[0].CorrelationId);
                Assert.AreEqual(environment, actual[0].Environment);
                Assert.AreEqual(application, actual[0].Application);
                Assert.AreEqual(logEvent.Level.ToString(), actual[0].Level);
                Assert.AreEqual(host, actual[0].MachineName);
                Assert.AreEqual("SourceContext", actual[0].SourceContext);
            }
        }
    }
}