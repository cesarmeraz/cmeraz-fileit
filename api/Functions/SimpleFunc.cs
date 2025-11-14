using System.Net;
using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using FileIt.App.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FileIt.Api.Functions;

public class SimpleFunc : BaseFunction
{
    private readonly ILogger<SimpleFunc> _logger;
    private readonly ISimpleService _simpleService;

    public SimpleFunc(ILogger<SimpleFunc> logger, ISimpleService simpleService)
        : base(logger)
    {
        _logger = logger;
        _simpleService = simpleService;
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
        LogFunctionStart(nameof(SeedSimple));
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
        [BlobTrigger("simple-source/{name}")] BlobClient blobClient,
        string name
    )
    {
        LogFunctionStart(nameof(ReceiveSimple));
        blobClient = blobClient ?? throw new ArgumentNullException(nameof(blobClient));
        
        // use the blobClient to get the x-ms-client-request-id property from the original request header
        var propsResponse = await blobClient.GetPropertiesAsync();
        var rawResponse = propsResponse.GetRawResponse();
        if (rawResponse.Headers.TryGetValue("x-ms-client-request-id", out var clientRequestId))
        {
            _logger.LogInformation("x-ms-client-request-id: {ClientRequestId}", clientRequestId);
        }
        else
        {
            _logger.LogInformation("x-ms-client-request-id header not found on GetProperties response.");
            clientRequestId= Guid.NewGuid().ToString();
        }

        await _simpleService.QueueAsync(name, clientRequestId);
        LogFunctionEnd(nameof(ReceiveSimple));
    }

    /// <summary>
    /// A ServiceBusTrigger that processes the file ingested
    /// </summary>
    /// <param name="message">the ServiceBusReceivedMessage</param>
    /// <returns></returns>
    [Function(nameof(ProcessSimple))]
    public async Task ProcessSimple([ServiceBusTrigger("simple")] ServiceBusReceivedMessage message)
    {
        LogFunctionStart(nameof(ProcessSimple));

        _logger.LogInformation($"Message ID: {message.MessageId}");
        _logger.LogInformation($"Message Body: {message.Body.ToString()}");
        _logger.LogInformation($"Message Content-Type: {message.ContentType}");
        // Process the Service Bus message here
        await _simpleService.ProcessAsync(message);
        LogFunctionEnd(nameof(ProcessSimple));
    }
}
