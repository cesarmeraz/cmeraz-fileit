using Azure.Storage.Blobs;

var builder = DistributedApplication.CreateBuilder(args);

// --- Azurite (Blob/Queue/Table) ---
var azurite = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithBlobPort(10000);
        emulator.WithQueuePort(10001);
        emulator.WithTablePort(10002);
    });

var blobs = azurite.AddBlobs("blobs");

// --- Azure SQL (cloud) ---
var azureSql = builder.AddConnectionString("azureSql");

// --- Func Apps with full wiring ---
var services = builder.AddProject<Projects.FileIt_Module_Services_Host>("services-host")
    .WithReference(blobs)
    .WithEnvironment("FileItDbConnection", azureSql)
    .WaitFor(blobs);

var simpleflow = builder.AddProject<Projects.FileIt_Module_SimpleFlow_Host>("simpleflow-host")
    .WithReference(blobs)
    .WithEnvironment("FileItDbConnection", azureSql)
    .WaitFor(services)
    .WaitFor(blobs);

var dataflow = builder.AddProject<Projects.FileIt_Module_DataFlow_Host>("dataflow-host")
    .WithReference(blobs)
    .WithEnvironment("FileItDbConnection", azureSql)
    .WaitFor(services)
    .WaitFor(blobs);

var app = builder.Build();

// --- Ensure DataFlow blob containers exist on startup ---
// Azurite emulator uses a well-known connection string
const string azuriteConnectionString =
    "DefaultEndpointsProtocol=http;" +
    "AccountName=devstoreaccount1;" +
    "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
    "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";

_ = Task.Run(async () =>
{
    // wait a few seconds for Azurite container to be ready
    await Task.Delay(TimeSpan.FromSeconds(8));

    var serviceClient = new BlobServiceClient(azuriteConnectionString);
    foreach (var containerName in new[]
    {
        "dataflow-source", "dataflow-working", "dataflow-final",
        "simple-source", "simple-working", "simple-final"
    })
    {
        try
        {
            await serviceClient.GetBlobContainerClient(containerName).CreateIfNotExistsAsync();
            Console.WriteLine($"[container-init] ensured: {containerName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[container-init] failed for {containerName}: {ex.Message}");
        }
    }
});

app.Run();
