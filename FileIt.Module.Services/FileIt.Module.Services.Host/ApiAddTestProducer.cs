using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FileIt.Domain.Entities.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;

namespace FileIt.Module.Services.Host;

/// <summary>
/// Demo / test trigger. POST a fileName, get back a correlationId, watch
/// it propagate through services-host -> complex-host (HTTP) and the
/// api-add-topic subscription on simpleflow-host. Lives here because
/// services-host already has the IAzureClientFactory<ServiceBusSender>
/// registration with name "api-add".
/// </summary>
public class ApiAddTestProducer
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAzureClientFactory<ServiceBusSender> _senderFactory;
    private readonly ILogger<ApiAddTestProducer> _logger;

    public ApiAddTestProducer(
        IAzureClientFactory<ServiceBusSender> senderFactory,
        ILogger<ApiAddTestProducer> logger)
    {
        _senderFactory = senderFactory;
        _logger = logger;
    }

    [Function("ApiAdd_Test_Producer")]
    public async Task<IActionResult> Post(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "test/api-add")]
            HttpRequest req,
        FunctionContext ctx)
    {
        var ct = ctx.CancellationToken;

        ApiAddPayload? payload;
        try
        {
            using var reader = new StreamReader(req.Body);
            var raw = await reader.ReadToEndAsync(ct);
            payload = string.IsNullOrWhiteSpace(raw)
                ? new ApiAddPayload { FileName = "demo.txt" }
                : JsonSerializer.Deserialize<ApiAddPayload>(raw, JsonOpts);
        }
        catch (JsonException ex)
        {
            return new BadRequestObjectResult(new { error = $"Invalid JSON: {ex.Message}" });
        }
        if (payload is null || string.IsNullOrWhiteSpace(payload.FileName))
        {
            payload = new ApiAddPayload { FileName = "demo.txt" };
        }

        var correlationId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        var bodyJson = JsonSerializer.Serialize(payload, JsonOpts);

        var sender = _senderFactory.CreateClient("api-add");
        var message = new ServiceBusMessage(bodyJson)
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            Subject = "api-add-simple",
            ContentType = "application/json",
        };

        _logger.LogInformation(
            "ApiAdd test producer publishing. CorrelationId={CorrelationId} FileName={FileName}",
            correlationId, payload.FileName);

        await sender.SendMessageAsync(message, ct);

        return new ObjectResult(new
        {
            status = "queued",
            correlationId,
            messageId,
            fileName = payload.FileName,
            queue = "api-add",
        })
        {
            StatusCode = (int)HttpStatusCode.Accepted,
        };
    }
}
