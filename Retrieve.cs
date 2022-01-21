using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace RetrieveApp
{
    public static class Retrieve
    {
        // This is the Azure sample function, leaving it as reference
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }

    // This function just test passing more than one parameter
    public static class ShowDate
    {
        [FunctionName("ShowDate")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string month = req.Query["month"];
            string day = req.Query["day"];

            string responseMessage = (string.IsNullOrEmpty(day) || string.IsNullOrEmpty(month)) ?
                "No full day given." : $"Given date {month}/{day}.";

            return new OkObjectResult(responseMessage);
        }
    }

    // This function downloads my pdf CV from a given blob storage
    // It needs down=cv as param otherwise it returns an error message
    public static class GetCV
    {
        [FunctionName("GetCV")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string download = req.Query["down"];
            string responseMessage = "";

            if (string.IsNullOrEmpty(download) || download.ToLowerInvariant() != "cv") {
                responseMessage = "Nothing to do";
                return new OkObjectResult(responseMessage);
            }

            PrivateDataModel model;

            // get needed strings
            try
            {
                string path = context.FunctionAppDirectory;
                StreamReader dataReader = new StreamReader(path + "\\private.json");
                string jsonString = dataReader.ReadToEnd();
                model = JsonConvert.DeserializeObject<PrivateDataModel>(jsonString);
            }catch (Exception e)
            {
                return new OkObjectResult("Failed to get valid parameters");
            }

            BlobClient client = new BlobClient(model.connectionString, "private-container", model.cvName);
            MemoryStream stream = new MemoryStream();

            client.DownloadTo(stream);
            byte[] filebytes = stream.ToArray();
            
            return new FileContentResult(filebytes, "application/pdf") {
                FileDownloadName = model.cvName
            };
        }
    }
}
