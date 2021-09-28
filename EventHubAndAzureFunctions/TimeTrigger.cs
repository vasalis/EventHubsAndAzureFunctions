using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace EventHubAndAzureFunctions
{
    public static class TimeTrigger
    {
        [FunctionName("TimeTrigger")]
        [return: EventHub("myeventhub", Connection = "EventHubConnectionString")]
        public static string Run([TimerTrigger("*/1 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            return $"{DateTime.Now}";
        }
    }
}
