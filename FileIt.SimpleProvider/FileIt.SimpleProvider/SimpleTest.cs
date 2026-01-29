using System.Text;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.SimpleProvider.App;

public class SimpleTest
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly SimpleConfig _config;
    private readonly ILogger<SimpleTest> _logger;

    public SimpleTest(
        ILogger<SimpleTest> logger,
        BlobServiceClient blobServiceClient,
        SimpleConfig config
    )
    {
        _blobServiceClient = blobServiceClient;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// A testing aid that uploads a file to the blob storage emulator
    /// </summary>
    /// <param name="req">the HttpRequestData</param>
    /// <param name="executionContext">the FunctionContext</param>
    /// <returns></returns>
    [Function(nameof(SimpleTest))]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        using (
            _logger!.BeginScope(
                new Dictionary<string, object>() { { "EventId", _config.SimpleTestEventId } }
            )
        )
        {
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

            _logger.LogInformation("Seeded blob '{name}' into container 'simple-source'.", name);
        }
    }
}
