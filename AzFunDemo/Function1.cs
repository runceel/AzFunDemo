using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace AzFunDemo
{
    public static class Function1
    {
        [FunctionName("AddData")]
        public static async Task<IActionResult> AddData(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                "azfundemoDb", 
                "sensors", 
                ConnectionStringSetting = "CosmosDb", 
                CreateIfNotExists = true, 
                Id = "id", 
                PartitionKey = "/sensor/id")] IAsyncCollector<SensorData> sensorData,
            ILogger log)
        {
            var inputs = JsonConvert.DeserializeObject<SensorData[]>(await req.ReadAsStringAsync());
            log.LogInformation("Received {0} items.", inputs.Length);
            foreach (var input in inputs)
            {
                await sensorData.AddAsync(input);
            }

            return new AcceptedResult();
        }

        [FunctionName("CheckValue")]
        public static async Task CheckValue(
            [CosmosDBTrigger(
                "azfundemoDb", 
                "sensors", 
                ConnectionStringSetting = "CosmosDb", 
                CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<Document> inputs,
            [SignalR(HubName = "alert")] IAsyncCollector<SignalRMessage> messages,
            ILogger log)
        {
            log.LogInformation("{0} items were added.", inputs.Count);
            var alertTargets = inputs.Select(x => (SensorData)(dynamic)x)
                .Where(x => x.Value >= 50);

            await messages.AddAsync(new SignalRMessage
            {
                Target = "alert",
                Arguments = new[] { alertTargets }
            });
        }

        [FunctionName("negotiate")]
        public static SignalRConnectionInfo Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            [SignalRConnectionInfo(HubName = "alert")] SignalRConnectionInfo connectionInfo)
            => connectionInfo;
    }

    public class SensorData
    {
        [JsonProperty("value")]
        public int Value { get; set; }
        [JsonProperty("dateTime")]
        public DateTimeOffset DateTime { get; set; }
        [JsonProperty("sensor")]
        public Sensor Sensor { get; set; }
    }

    public class Sensor
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
