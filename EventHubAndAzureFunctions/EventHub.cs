using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace EventHubAndAzureFunctions
{
    public static class EventHub
    {
        [FunctionName("EventHub")]
        public static async Task Run([EventHubTrigger("myeventhub", Connection = "EventHubConnectionString")] EventData[] events, ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"EventHub v2 processed a message: {messageBody}");
                    GetSomethingFromDb(log);
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }

        private static async Task<string> GetSomethingFromDb(ILogger log)
        {
            string responseMessage = "";
            try
            {
                string lConnectionString = Environment.GetEnvironmentVariable("SQLConString");
                using (SqlConnection connection = new SqlConnection(lConnectionString))
                {
                    connection.Open();
                    SqlCommand command = new SqlCommand($"SELECT * FROM SensitiveDataTable", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string returnString = string.Empty;
                            responseMessage += $"Name : {reader["CreditCardInfo"]}. ";
                            responseMessage += $"Description : {reader["SocialSecurityNumber"]}";
                        }
                        else
                        {
                            responseMessage = "EMPTY SQL";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                responseMessage = $"SQL failed: {ex.Message}";
                log.LogError(ex, responseMessage);
            }

            return responseMessage;
        }
    }
}
