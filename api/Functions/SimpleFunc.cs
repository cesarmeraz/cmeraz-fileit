using System.Net;
using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.App.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FileIt.Api.Functions;

public class SimpleFunc
{
    private readonly ILogger<SimpleFunc> _logger;
    private readonly ISimpleService _blobService;

    public SimpleFunc(ILogger<SimpleFunc> logger, ISimpleService blobService)
    {
        _logger = logger;
        _blobService = blobService;
    }

    /// <summary>
    /// A testing aid that uploads a file to the blob storage emulator
    /// </summary>
    /// <param name="req">the HttpRequestData</param>
    /// <param name="executionContext">the FunctionContext</param>
    /// <returns></returns>
    [Function(nameof(SeedSimple))]
    public async Task<HttpResponseData> SeedSimple(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequestData req,
        FunctionContext executionContext
    )
    {
        // Create a small seeded file and upload it to the 'simple-source' container
        var name = $"seed-{Guid.NewGuid()}.txt";
        var content = $"Seeded blob created at {DateTime.UtcNow:o}";

        // Resolve BlobServiceClient from the function's service provider (registered in Program.cs)
        var sp = executionContext.InstanceServices;
        var blobServiceClient = sp.GetService(typeof(BlobServiceClient)) as BlobServiceClient;

        if (blobServiceClient == null)
        {
            _logger.LogError("BlobServiceClient is not available from DI. Cannot seed blob.");
            var errorResp = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResp.WriteString("BlobServiceClient not configured.");
            return errorResp;
        }

        var containerClient = blobServiceClient.GetBlobContainerClient("simple-source");
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(name);
        var bytes = Encoding.UTF8.GetBytes(content);
        using var ms = new MemoryStream(bytes);
        await blobClient.UploadAsync(ms, overwrite: true);

        _logger.LogInformation($"Seeded blob '{name}' into container 'simple-source'.");

        var resp = req.CreateResponse(HttpStatusCode.OK);
        resp.WriteString($"Uploaded blob: {name}");
        return resp;
    }

    /// <summary>
    /// a BlobTrigger that receives the file stream and its name
    /// </summary>
    /// <param name="stream">the file content</param>
    /// <param name="name">the file name</param>
    /// <returns></returns>
    [Function(nameof(ReceiveSimple))]
    public async Task ReceiveSimple(
        [BlobTrigger("simple-source/{name}", Connection = "AzureWebJobsStorage")] Stream stream,
        string name
    )
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation(
            "C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}",
            name,
            content
        );
        var isValid = await _blobService.ValidateBlobAsync(stream, name);
        if (isValid)
        {
            _logger.LogInformation($"Blob {name} is valid.");
            await _blobService.QueueAsync(name);
        }
        else
        {
            _logger.LogWarning($"Blob {name} is invalid.");
        }
    }

    /// <summary>
    /// A ServiceBusTrigger that processes the file ingested
    /// </summary>
    /// <param name="message">the ServiceBusReceivedMessage</param>
    /// <returns></returns>
    [Function(nameof(ProcessSimple))]
    public async Task ProcessSimple(
        [ServiceBusTrigger("simple", Connection = "ServiceBusConnectionString")]
            ServiceBusReceivedMessage message
    )
    {
        _logger.LogInformation($"Message ID: {message.MessageId}");
        _logger.LogInformation($"Message Body: {message.Body.ToString()}");
        _logger.LogInformation($"Message Content-Type: {message.ContentType}");
        // Process the Service Bus message here
        await _blobService.ProcessAsync(message);
    }
}
