//using Azure.Messaging.ServiceBus;
using FileIt.App.Services;
using Microsoft.Azure.Functions.Worker;
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
            //await _blobService.QueueAsync(stream, name);
        }
        else
        {
            _logger.LogWarning($"Blob {name} is invalid.");
        }
    }

    // [Function(nameof(ProcessSimple))]
    // public async Task ProcessSimple(
    //     [ServiceBusTrigger("simple", Connection = "ServiceBusConnection")]
    //         ServiceBusReceivedMessage message
    // )
    // {
    //     _logger.LogInformation($"Message ID: {message.MessageId}");
    //     _logger.LogInformation($"Message Body: {message.Body.ToString()}");
    //     _logger.LogInformation($"Message Content-Type: {message.ContentType}");
    //     // Process the Service Bus message here
    //     await _blobService.ProcessAsync(message);
    //     await Task.CompletedTask;
    // }
}
