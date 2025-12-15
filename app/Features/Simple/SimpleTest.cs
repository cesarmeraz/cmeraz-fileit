using System.Text;
using Azure.Storage.Blobs;
using FileIt.App.Functions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.App.Features.Simple;

public class SimpleTest : BaseFunction
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly SimpleConfig _config;

    public SimpleTest(
        ILogger<SimpleTest> logger,
        BlobServiceClient blobServiceClient,
        SimpleConfig config
    )
        : base(logger, nameof(SimpleTest))
    {
        _blobServiceClient = blobServiceClient;
        _config = config;
    }

    /// <summary>
    /// A testing aid that uploads a file to the blob storage emulator
    /// </summary>
    /// <param name="req">the HttpRequestData</param>
    /// <param name="executionContext">the FunctionContext</param>
    /// <returns></returns>
    [Function(nameof(SimpleTest))]
    public async Task Run([TimerTrigger("%SimpleTestCron%")] TimerInfo myTimer)
    {
        using (
            logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "EventId", _config.SimpleTestEventId },
                    { "Feature", _config.FeatureName },
                }
            )
        )
        {
            LogFunctionStart(nameof(SimpleTest));
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

            logger.LogInformation("Seeded blob '{name}' into container 'simple-source'.", name);
            LogFunctionEnd(nameof(SimpleTest));
        }
    }
}
