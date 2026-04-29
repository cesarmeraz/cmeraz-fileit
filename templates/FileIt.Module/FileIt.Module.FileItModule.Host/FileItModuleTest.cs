using System.Text;
using Azure.Storage.Blobs;
using FileIt.Module.FileItModule.App;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.FileItModule;

/// <summary>
/// Local-dev aid that drops a test blob into the source container every minute
/// to drive the watcher pipeline without manual uploads. Disable in production
/// by removing the schedule env var or setting it to a never-firing CRON.
/// </summary>
public class FileItModuleTest
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly FileItModuleConfig _config;
    private readonly ILogger<FileItModuleTest> _logger;

    public FileItModuleTest(
        ILogger<FileItModuleTest> logger,
        BlobServiceClient blobServiceClient,
        FileItModuleConfig config)
    {
        _blobServiceClient = blobServiceClient;
        _config = config;
        _logger = logger;
    }

    [Function(nameof(FileItModuleTest))]
    public async Task Run([TimerTrigger("%EveryMinuteSchedule%")] TimerInfo myTimer)
    {
        using (_logger.BeginScope(
            new Dictionary<string, object>() { { "EventId", FileItModuleEvents.FileItModuleTest } }))
        {
            var name = $"test-{Guid.NewGuid()}.txt";
            var content = $"Seeded blob created at {DateTime.Now}";
            var container = _config.SourceContainer;

            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var exists = await containerClient.ExistsAsync();
            if (!exists)
                await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(name);
            var bytes = Encoding.UTF8.GetBytes(content);
            using var ms = new MemoryStream(bytes);
            await blobClient.UploadAsync(ms, overwrite: true);

            _logger.LogInformation(
                "Seeded blob '{FileName}' into container '{Container}'.",
                name,
                container);
        }
    }
}
