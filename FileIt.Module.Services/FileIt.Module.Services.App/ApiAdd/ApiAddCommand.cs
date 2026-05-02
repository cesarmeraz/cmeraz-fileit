using System.Text.Json;
using FileIt.Domain.Entities.Api;
using FileIt.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Services.App.ApiAdd;

public interface IApiAddCommand
{
    Task ApiAdd(ApiRequest request, CancellationToken cancellationToken = default);
}

public class ApiAddCommand : IApiAddCommand
{
    private readonly IApiLogRepo _apiLogRepo;
    private readonly IBroadcastResponses _broadcaster;
    private readonly IComplexApiClient _complexApi;
    private readonly ILogger<ApiAddCommand> _logger;

    public ApiAddCommand(
        IApiLogRepo apiLogRepo,
        ILogger<ApiAddCommand> logger,
        IBroadcastResponses broadcaster,
        IComplexApiClient complexApi
    )
    {
        _broadcaster = broadcaster;
        _apiLogRepo = apiLogRepo;
        _logger = logger;
        _complexApi = complexApi;
    }

    public async Task ApiAdd(ApiRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(ServicesEvents.LogApiAddRequest, "Calling Complex API");

        // Forward the request to the Complex module. The Services module's
        // job here is to delegate, not to invent content, so we pack the
        // incoming ApiRequest as the document body and use CorrelationId as
        // the idempotency key so broker retries are safe.
        var docName = $"svc-{request.CorrelationId ?? Guid.NewGuid().ToString()}.json";
        var docBody = JsonSerializer.Serialize(request);

        // Note: we deliberately do NOT catch ComplexApiUnavailableException.
        // A 503 from the chaos layer should bubble so the Service Bus
        // dispatcher retries the message. That IS the chaos demo for #10.
        var result = await _complexApi.CreateDocumentAsync(
            docName,
            "application/json",
            docBody,
            idempotencyKey: request.CorrelationId,
            cancellationToken: cancellationToken
        );

        cancellationToken.ThrowIfCancellationRequested();

        var apiLogItem = await _apiLogRepo.AddAsync(
            request.CorrelationId ?? string.Empty,
            "Request body",
            "Response body",
            $"Complex:{result.Id}"
        );

        var response = new ApiAddResponse()
        {
            NodeId = apiLogItem!.Id,
            CorrelationId = request.CorrelationId,
            TopicName = request.ReplyTo!,
        };

        _logger.LogDebug(
            ServicesEvents.ApiAddResponsePublished,
            "Response published:\n{@ApiAddResponse}",
            response
        );
        await _broadcaster.EmitAsync(response, cancellationToken);
    }
}
