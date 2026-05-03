using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FileIt.Domain.Interfaces;
using FileIt.Infrastructure.HttpClients;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FileIt.Infrastructure.Test.HttpClients;

[TestClass]
public class TestComplexApiClient
{
    public required Mock<HttpMessageHandler> _handlerMock;
    public required HttpClient _httpClient;
    public required Mock<ILogger<ComplexApiClient>> _loggerMock;
    public required ComplexApiClient target;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [TestInitialize]
    public void Setup()
    {
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:7064/"),
        };
        _loggerMock = new Mock<ILogger<ComplexApiClient>>();
        _loggerMock.Setup(m =>
            m.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
            )
        );

        target = new ComplexApiClient(_httpClient, _loggerMock.Object);
    }

    private void SetupHandlerResponse(
        HttpStatusCode status,
        object? jsonBody = null,
        string? locationHeader = null,
        TimeSpan? retryAfter = null,
        Action<HttpRequestMessage>? captureRequest = null)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                captureRequest?.Invoke(req);
                var response = new HttpResponseMessage(status);
                if (jsonBody != null)
                {
                    response.Content = new StringContent(
                        JsonSerializer.Serialize(jsonBody, JsonOpts),
                        Encoding.UTF8,
                        "application/json");
                }
                if (locationHeader != null)
                {
                    response.Headers.Location = new Uri(locationHeader, UriKind.Relative);
                }
                if (retryAfter.HasValue)
                {
                    response.Headers.RetryAfter =
                        new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter.Value);
                }
                return Task.FromResult(response);
            });
    }

    // ---- CreateDocumentAsync: happy path ----

    [TestMethod]
    public async Task CreateDocumentAsync_HappyPath_ReturnsParsedResult()
    {
        var docId = Guid.NewGuid();
        SetupHandlerResponse(HttpStatusCode.Created,
            jsonBody: new
            {
                id = docId,
                name = "test.txt",
                contentType = "text/plain",
                sizeBytes = 100,
                createdUtc = DateTime.UtcNow,
                modifiedUtc = DateTime.UtcNow,
            },
            locationHeader: $"/api/documents/{docId}");

        var result = await target.CreateDocumentAsync("test.txt", "text/plain", "hello");

        Assert.AreEqual(docId, result.Id);
        StringAssert.Contains(result.Location, docId.ToString());
        Assert.IsFalse(result.WasIdempotentReplay);
    }

    [TestMethod]
    public async Task CreateDocumentAsync_PostsToCorrectEndpoint()
    {
        HttpRequestMessage? captured = null;
        SetupHandlerResponse(HttpStatusCode.Created,
            jsonBody: new { id = Guid.NewGuid(), name = "x", contentType = "text/plain", sizeBytes = 1L, createdUtc = DateTime.UtcNow, modifiedUtc = DateTime.UtcNow },
            captureRequest: req => captured = req);

        await target.CreateDocumentAsync("x", "text/plain", "x");

        Assert.IsNotNull(captured);
        Assert.AreEqual(HttpMethod.Post, captured!.Method);
        Assert.AreEqual("api/documents", captured.RequestUri!.PathAndQuery.TrimStart('/'));
    }

    [TestMethod]
    public async Task CreateDocumentAsync_AddsIdempotencyKeyHeader()
    {
        HttpRequestMessage? captured = null;
        SetupHandlerResponse(HttpStatusCode.Created,
            jsonBody: new { id = Guid.NewGuid(), name = "x", contentType = "text/plain", sizeBytes = 1L, createdUtc = DateTime.UtcNow, modifiedUtc = DateTime.UtcNow },
            captureRequest: req => captured = req);

        await target.CreateDocumentAsync("x", "text/plain", "x", idempotencyKey: "key-abc");

        Assert.IsNotNull(captured);
        Assert.IsTrue(captured!.Headers.Contains("Idempotency-Key"));
        Assert.AreEqual("key-abc", captured.Headers.GetValues("Idempotency-Key").Single());
    }

    [TestMethod]
    public async Task CreateDocumentAsync_NullIdempotencyKey_DoesNotSendHeader()
    {
        HttpRequestMessage? captured = null;
        SetupHandlerResponse(HttpStatusCode.Created,
            jsonBody: new { id = Guid.NewGuid(), name = "x", contentType = "text/plain", sizeBytes = 1L, createdUtc = DateTime.UtcNow, modifiedUtc = DateTime.UtcNow },
            captureRequest: req => captured = req);

        await target.CreateDocumentAsync("x", "text/plain", "x", idempotencyKey: null);

        Assert.IsNotNull(captured);
        Assert.IsFalse(captured!.Headers.Contains("Idempotency-Key"));
    }

    [TestMethod]
    public async Task CreateDocumentAsync_EmptyIdempotencyKey_DoesNotSendHeader()
    {
        HttpRequestMessage? captured = null;
        SetupHandlerResponse(HttpStatusCode.Created,
            jsonBody: new { id = Guid.NewGuid(), name = "x", contentType = "text/plain", sizeBytes = 1L, createdUtc = DateTime.UtcNow, modifiedUtc = DateTime.UtcNow },
            captureRequest: req => captured = req);

        await target.CreateDocumentAsync("x", "text/plain", "x", idempotencyKey: "");

        Assert.IsNotNull(captured);
        Assert.IsFalse(captured!.Headers.Contains("Idempotency-Key"));
    }

    // ---- CreateDocumentAsync: 503 chaos failure ----

    [TestMethod]
    public async Task CreateDocumentAsync_503_ThrowsComplexApiUnavailableException()
    {
        SetupHandlerResponse(HttpStatusCode.ServiceUnavailable, retryAfter: TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<ComplexApiUnavailableException>(
            () => target.CreateDocumentAsync("x", "text/plain", "x"));
    }

    [TestMethod]
    public async Task CreateDocumentAsync_503_ExceptionMessageIncludesRetryAfter()
    {
        SetupHandlerResponse(HttpStatusCode.ServiceUnavailable, retryAfter: TimeSpan.FromSeconds(7));

        try
        {
            await target.CreateDocumentAsync("x", "text/plain", "x");
            Assert.Fail("Expected ComplexApiUnavailableException");
        }
        catch (ComplexApiUnavailableException ex)
        {
            StringAssert.Contains(ex.Message, "7");
        }
    }

    [TestMethod]
    public async Task CreateDocumentAsync_503_NoRetryAfterHeader_DefaultsTo2Seconds()
    {
        SetupHandlerResponse(HttpStatusCode.ServiceUnavailable);

        try
        {
            await target.CreateDocumentAsync("x", "text/plain", "x");
            Assert.Fail("Expected ComplexApiUnavailableException");
        }
        catch (ComplexApiUnavailableException ex)
        {
            StringAssert.Contains(ex.Message, "2");
        }
    }

    // ---- CreateDocumentAsync: other failures ----

    [TestMethod]
    public async Task CreateDocumentAsync_500_ThrowsHttpRequestException()
    {
        SetupHandlerResponse(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => target.CreateDocumentAsync("x", "text/plain", "x"));
    }

    [TestMethod]
    public async Task CreateDocumentAsync_422_ThrowsHttpRequestException()
    {
        SetupHandlerResponse(HttpStatusCode.UnprocessableEntity);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => target.CreateDocumentAsync("x", "text/plain", "x"));
    }

    [TestMethod]
    public async Task CreateDocumentAsync_CancellationRequested_PropagatesOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => target.CreateDocumentAsync("x", "text/plain", "x", cancellationToken: cts.Token));
    }

    // ---- GetDocumentAsync ----

    [TestMethod]
    public async Task GetDocumentAsync_HappyPath_ReturnsParsedDto()
    {
        var docId = Guid.NewGuid();
        var created = DateTime.UtcNow.AddMinutes(-5);
        var modified = DateTime.UtcNow;
        SetupHandlerResponse(HttpStatusCode.OK,
            jsonBody: new
            {
                id = docId,
                name = "doc.txt",
                contentType = "text/plain",
                sizeBytes = 12345L,
                createdUtc = created,
                modifiedUtc = modified,
            });

        var dto = await target.GetDocumentAsync(docId);

        Assert.IsNotNull(dto);
        Assert.AreEqual(docId, dto!.Id);
        Assert.AreEqual("doc.txt", dto.Name);
        Assert.AreEqual("text/plain", dto.ContentType);
        Assert.AreEqual(12345L, dto.SizeBytes);
    }

    [TestMethod]
    public async Task GetDocumentAsync_404_ReturnsNull()
    {
        SetupHandlerResponse(HttpStatusCode.NotFound);

        var dto = await target.GetDocumentAsync(Guid.NewGuid());

        Assert.IsNull(dto);
    }

    [TestMethod]
    public async Task GetDocumentAsync_500_ThrowsHttpRequestException()
    {
        SetupHandlerResponse(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => target.GetDocumentAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task GetDocumentAsync_GetsToCorrectEndpoint()
    {
        HttpRequestMessage? captured = null;
        var docId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        SetupHandlerResponse(HttpStatusCode.OK,
            jsonBody: new { id = docId, name = "x", contentType = "text/plain", sizeBytes = 1L, createdUtc = DateTime.UtcNow, modifiedUtc = DateTime.UtcNow },
            captureRequest: req => captured = req);

        await target.GetDocumentAsync(docId);

        Assert.IsNotNull(captured);
        Assert.AreEqual(HttpMethod.Get, captured!.Method);
        StringAssert.Contains(captured.RequestUri!.PathAndQuery, docId.ToString());
    }
}
