using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookService
{
    public class BookDTO
    {
        [JsonProperty("isbn")]
        public string ISBN { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("publishedYear")]
        public string PublishedYear { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }
    }
}
