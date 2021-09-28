using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using System.Text;

namespace EventHubAndAzureFunctions
{
    public static class RestApi
    {
        [FunctionName("CreateEvents")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CreateEvents called");

            string lNumOfEventsStr = req.Query["numofevents"];

            int lNumOfEvents = 1000;

            if (!string.IsNullOrEmpty(lNumOfEventsStr))
            {
                lNumOfEvents = Convert.ToInt32(lNumOfEventsStr);
            }

            string responseMessage = $"Created: {lNumOfEvents} events";

            SendToEventHub(lNumOfEvents, log);


            return new OkObjectResult(responseMessage);
        }

        [FunctionName("CheckDbConnection")]
        public static async Task<IActionResult> Run2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CheckDbConnection called");

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
                            responseMessage = "Db result is: ";
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
            }

            return new OkObjectResult(responseMessage);
        }

        private static async void SendToEventHub(int aMsgCount, ILogger log)
        {
            try
            {
                if (aMsgCount == 0)
                {
                    aMsgCount = 1000;
                }

                // Create a producer client that you can use to send events to an event hub
                string lConnectionString = Environment.GetEnvironmentVariable("EventHubConnectionString");
                EventHubProducerClient producerClient = new EventHubProducerClient(lConnectionString, "myeventhub");

                // Create a batch of events 
                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                for (int i = 1; i <= aMsgCount; i++)
                {
                    if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"Event {i}"))))
                    {
                        // if it is too large for the batch
                        throw new Exception($"Event {i} is too large for the batch and cannot be sent.");
                    }
                }

                try
                {
                    // Use the producer client to send the batch of events to the event hub
                    await producerClient.SendAsync(eventBatch);
                    log.LogInformation($"A batch of {aMsgCount} events has been published.");
                }
                finally
                {
                    await producerClient.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Event Hub message creation failed!");
            }
        }
    }
}
