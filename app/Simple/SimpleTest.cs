using System.Text;
using Azure.Storage.Blobs;
using FileIt.App.Functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Simple;

public class SimpleTest : BaseFunction
{
    private readonly BlobServiceClient _blobServiceClient;

    public SimpleTest(ILogger<SimpleIntake> logger, BlobServiceClient blobServiceClient)
        : base(logger, nameof(SimpleIntake))
    {
        _blobServiceClient = blobServiceClient;
    }

    /// <summary>
    /// A testing aid that uploads a file to the blob storage emulator
    /// </summary>
    /// <param name="req">the HttpRequestData</param>
    /// <param name="executionContext">the FunctionContext</param>
    /// <returns></returns>
    [Function(nameof(SimpleTest))]
    public async Task Run([TimerTrigger("0 */2 * * * *")] TimerInfo myTimer)
    {
        LogFunctionStart(nameof(SimpleTest));
        // Create a small seeded file and upload it to the 'simple-source' container
        var name = $"test-{Guid.NewGuid()}.txt";
        var content = $"Seeded blob created at {DateTime.Now}";

        var containerClient = _blobServiceClient.GetBlobContainerClient("simple-source");
        var exists = await containerClient.ExistsAsync();
        if (!exists)
            await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(name);
        var bytes = Encoding.UTF8.GetBytes(content);
        using var ms = new MemoryStream(bytes);
        await blobClient.UploadAsync(ms, overwrite: true);

        logger.LogInformation($"Seeded blob '{name}' into container 'simple-source'.");
        LogFunctionEnd(nameof(SimpleTest));
    }
}
