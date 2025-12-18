using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace FileIt.SqlProvider.Functions;

public partial class BaseFunction
{
    private readonly int EventId = 1000;
    public readonly string FeatureName;
    public ILogger logger;

    public BaseFunction(ILogger logger, string featureName)
    {
        this.logger = logger;
        FeatureName = featureName;
    }

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Function Start: {FunctionName}"
    )]
    public partial void LogFunctionStart(string functionName);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Function End: {FunctionName}"
    )]
    public partial void LogFunctionEnd(string functionName);

    protected async Task<string> GetClientRequestIdFromHeaderAsync(BlobClient blobClient)
    {
        string? clientRequestId;
        using (
            logger!.BeginScope(
                new Dictionary<string, object>()
                {
                    { "EventId", EventId },
                    { "Feature", FeatureName },
                }
            )
        )
        {
            var propsResponse = await blobClient.GetPropertiesAsync();
            var rawResponse = propsResponse.GetRawResponse();
            if (rawResponse.Headers.TryGetValue("x-ms-client-request-id", out clientRequestId))
            {
                this.logger.LogInformation(
                    "x-ms-client-request-id: {ClientRequestId}",
                    clientRequestId
                );
            }
            else
            {
                this.logger.LogInformation(
                    "x-ms-client-request-id header not found on GetProperties response."
                );
                clientRequestId = Guid.NewGuid().ToString();
            }
        }
        return clientRequestId!;
    }
}
