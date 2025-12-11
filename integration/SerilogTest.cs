using Microsoft.Extensions.Logging;

namespace FileIt.Integration.Test
{
    [TestClass]
    public class SerilogTest
    {
        [TestMethod]
        public void TestAdd()
        {
            string ClientRequestId = Guid.NewGuid().ToString(),
                Module = "TestAdd";
            int EventId = 1;

            using var host = TestHost.CreateHost();

            ILogger<SerilogTest>? logger =
                host.Services.GetService(typeof(ILogger<SerilogTest>)) as ILogger<SerilogTest>;
            logger!.BeginScope(
                new Dictionary<string, object>() { { "ClientRequestId", ClientRequestId } }
            );
            logger!.BeginScope(
                new Dictionary<string, object>() { { "EventId", EventId }, { "Module", Module } }
            );
            logger!.LogInformation("This is an information statement.");
            logger!.LogWarning("This is a warning statement.");
            logger!.Log(LogLevel.Warning, "This is a log statement");
        }
    }
}
