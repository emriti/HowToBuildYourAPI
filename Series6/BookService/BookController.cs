using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Client;
using System.Linq;
using Microsoft.Azure.Documents;

namespace BookService
{
    public static class BookController
    {
        [FunctionName("GetAllBooks")]
        public static async Task<IActionResult> GetAllBooks(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Book/all")] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosDB")] DocumentClient client,
            ILogger log)
        {
            var results = client.CreateDocumentQuery(UriFactory.CreateDocumentCollectionUri("Books", "Book"),
                "Select * from c",
                new FeedOptions() { EnableCrossPartitionQuery = true })
            .AsEnumerable();
            return new OkObjectResult(results);
        }

        [FunctionName("GetBookById")]
        public static async Task<IActionResult> GetBookById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Book/{year}/{id}")] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosDB")] DocumentClient client,
            string year,
            string id,
            ILogger log)
        {
            var oldBook = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri("Books", "Book", id),
                new RequestOptions() { PartitionKey = new PartitionKey($"Book/{year}") });

            if (oldBook == null)
            {
                return new BadRequestObjectResult("Book not found");
            }
            return new OkObjectResult(oldBook.Resource);
        }

        [FunctionName("AddBook")]
        public static async Task<IActionResult> AddBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Book")] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosDB")] DocumentClient client,
            ILogger log)
        {
            String requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            BookDTO book = JsonConvert.DeserializeObject<BookDTO>(requestBody);
            if (book != null)
            {
                book.PartitionKey = $"Book/{book.PublishedYear}";
                await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("Books", "Book"), book);

                return new OkObjectResult(book);
            }
            return new BadRequestObjectResult("Book information is null");
        }

        [FunctionName("UpdateBook")]
        public static async Task<IActionResult> UpdateBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Book/{id}")] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosDB")] DocumentClient client,
            string id,
            ILogger log)
        {
            String requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            BookDTO updatedBook = JsonConvert.DeserializeObject<BookDTO>(requestBody);

            var oldBook = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri("Books", "Book", updatedBook.Id),
                new RequestOptions() { PartitionKey = new PartitionKey(updatedBook.PartitionKey) });

            if (oldBook == null)
            {
                return new BadRequestObjectResult("Book not found");
            }
            else if (updatedBook != null & oldBook != null)
            {
                await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri("Books", "Book", updatedBook.Id), updatedBook,
                    new RequestOptions() { PartitionKey = new PartitionKey(updatedBook.PartitionKey) });
                return new OkObjectResult(updatedBook);
            }
            return new BadRequestObjectResult("Book information is null");
        }

        [FunctionName("DeleteBook")]
        public static async Task<IActionResult> DeleteBook(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Book/{year}/{id}")] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosDB")] DocumentClient client,
            string year,
            string id,
            ILogger log)
        {
            var oldBook = await client.ReadDocumentAsync(UriFactory.CreateDocumentUri("Books", "Book", id),
                new RequestOptions() { PartitionKey = new PartitionKey($"Book/{year}") });

            if (oldBook == null)
            {
                return new BadRequestObjectResult("Book not found");
            }
            else
            {
                await client.DeleteDocumentAsync(UriFactory.CreateDocumentUri("Books", "Book", id), 
                    new RequestOptions() { PartitionKey = new PartitionKey($"Book/{year}") });
                return new OkObjectResult("Book deleted");
            }

        }
    }
}
