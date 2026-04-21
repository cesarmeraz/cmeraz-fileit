var builder = DistributedApplication.CreateBuilder(args);

var azurite = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithBlobPort(10000);
        emulator.WithQueuePort(10001);
        emulator.WithTablePort(10002);
    });

var blobs = azurite.AddBlobs("blobs");

var services = builder.AddProject<Projects.FileIt_Module_Services_Host>("services-host")
    .WaitFor(blobs);

var simpleflow = builder.AddProject<Projects.FileIt_Module_SimpleFlow_Host>("simpleflow-host")
    .WaitFor(services)
    .WaitFor(blobs);

var dataflow = builder.AddProject<Projects.FileIt_Module_DataFlow_Host>("dataflow-host")
    .WaitFor(services)
    .WaitFor(blobs);

builder.Build().Run();
