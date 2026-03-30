using System;
using System.Collections.Generic;
using System.Reflection;
using FileIt.Domain.Entities;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.Logging;
using Microsoft.Data.SqlClient;
using Serilog.Events;
using Serilog.Parsing;

namespace FileIt.Infrastructure.Integration;

[TestClass]
public class DatabaseSinkTest
{
    [TestMethod]
    public void TestEmit()
    {
        string correlationId = Guid.NewGuid().ToString();
        string connString =
            "Data Source=localhost;Initial Catalog=FileIt;User ID=FileItDev;Password=123qwe!@#QWE;TrustServerCertificate=True;Encrypt=True;";
        ICommonLogConfig featureConfig = new CommonLogConfig()
        {
            Agent = "Integration",
            Application = "FileIt.Common.Integration",
            Environment = "LocalDev",
            Host = "cesario",
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
                    // Open the database connection
                    connection.Open();

                    // Execute the command and obtain a SqlDataReader for the results
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // Read data row by row
                        while (reader.Read())
                        {
                            var log = new CommonLog()
                            {
                                CorrelationId = reader["CorrelationId"].ToString(),
                                Environment = reader["Environment"].ToString(),
                                Application = reader["Feature"].ToString(),
                                Level = reader["Level"].ToString(),
                                MachineName = reader["MachineName"].ToString(),
                                SourceContext = reader["SourceContext"].ToString(),
                                Message = reader["Message"].ToString(),
                                ApplicationVersion = reader["FeatureVersion"].ToString(),
                            };
                            actual.Add(log);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                Assert.AreEqual(1, actual.Count);
                Assert.AreEqual(correlationId, actual[0].CorrelationId);
                Assert.AreEqual("LocalDev", actual[0].Environment);
                // Assert.AreEqual("FileIt.Common.Integration", actual[0].Feature);
                Assert.AreEqual(logEvent.Level.ToString(), actual[0].Level);
                Assert.AreEqual("cesario", actual[0].MachineName);
                Assert.AreEqual("SourceContext", actual[0].SourceContext);
            }
        }
    }
}
