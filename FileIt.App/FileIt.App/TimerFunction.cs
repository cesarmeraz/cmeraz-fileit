using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.App
{
    public class MyTimerFunction
    {
        private readonly ILogger<MyTimerFunction> log;

        public MyTimerFunction(ILogger<MyTimerFunction> log)
        {
            this.log = log;
        }

        //Useful for testing the loggers
        //[Function("MyScheduledFunction")]
        public void Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            // The cron expression "0 */5 * * * *" triggers the function every 5 minutes.
            // For example, at 00:00:00, 00:05:00, 00:10:00, etc.

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.IsPastDue)
            {
                log.LogWarning("Timer is past due!");
            }

            // Add your business logic here.
            // This could involve calling other services, processing data, etc.
            log.LogInformation("Performing scheduled task...");
        }
    }
}
