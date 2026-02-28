using Azure.Storage.Blobs;

namespace FileIt.Infrastructure.Extensions;

public static class BlobClientExtensions
{
    public static async Task<string> GetCorrelationId(this BlobClient blobClient)
    {
        string? correlationId;
        var propsResponse = await blobClient.GetPropertiesAsync();
        var rawResponse = propsResponse.GetRawResponse();
        if (!rawResponse.Headers.TryGetValue("x-ms-client-request-id", out correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }
        return correlationId!;
    }
}
