using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using Azure.Messaging.EventHubs;
using System.Text;

namespace LoanService
{
    public static class LoanController
    {
        [FunctionName("ReturningABook")]
        public static async Task<IActionResult> ReturningABook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Loan/Return/{id}")] HttpRequest req,
            [CosmosDB()] DocumentClient client,
            string id,
            ILogger log)
        {
            // Get loan information
            var loan = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri("Loans", "Loan", id), 
                new RequestOptions() { PartitionKey = new PartitionKey("Loan") });

            await using (EventHubProducerClient evhClient = new EventHubProducerClient(
                Environment.GetEnvironmentVariable("LoanEventHubConn"), 
                Environment.GetEnvironmentVariable("LoanEventHubName")))
            {
                using EventDataBatch eventBatch = await evhClient.CreateBatchAsync();
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(loan.Resource))));
                await evhClient.SendAsync(eventBatch);
            }

            return new OkObjectResult("Success returning the book");
        }
    }
}
