using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FileIt.Module.Complex.App;

namespace FileIt.Module.Complex.Integration;

/// <summary>
/// Smoke tests that hit the live function host. Designed to run as part of
/// a manual integration pass:
///   1. Start FileIt.Module.Complex.Host (via dotnet run or func host start).
///   2. Run these tests with COMPLEX_API_BASE_URL set to the host URL.
/// They are skipped automatically if the env var is missing so CI doesn't
/// fail when the host isn't running.
/// </summary>
[TestClass]
public class SmokeTests
{
    private static string? BaseUrl =>
        Environment.GetEnvironmentVariable("COMPLEX_API_BASE_URL");

    private static HttpClient NewClient()
    {
        var c = new HttpClient { BaseAddress = new Uri(BaseUrl!) };
        c.DefaultRequestHeaders.Accept.Add(new("application/json"));
        return c;
    }

    [TestMethod]
    public async Task Health_ReturnsOk()
    {
        if (string.IsNullOrEmpty(BaseUrl)) return;
        using var client = NewClient();
        var resp = await client.GetAsync("api/health");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
    }

    [TestMethod]
    public async Task Swagger_Spec_Available()
    {
        if (string.IsNullOrEmpty(BaseUrl)) return;
        using var client = NewClient();
        var resp = await client.GetAsync("api/docs/swagger.json");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        StringAssert.Contains(body, "\"openapi\"");
        StringAssert.Contains(body, "FileIt Complex API");
    }

    [TestMethod]
    public async Task Create_Then_Get_RoundTrip()
    {
        if (string.IsNullOrEmpty(BaseUrl)) return;
        using var client = NewClient();

        var create = new CreateDocumentRequest
        {
            Name = "smoke.txt",
            ContentType = "text/plain",
            Content = "smoke test " + Guid.NewGuid(),
        };
        var resp = await client.PostAsJsonAsync("api/documents", create);

        // Chaos may occasionally return 503 even in dev. Tolerate it once.
        if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            await Task.Delay(2000);
            resp = await client.PostAsJsonAsync("api/documents", create);
        }

        Assert.AreEqual(HttpStatusCode.Created, resp.StatusCode);
        var created = await resp.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.IsNotNull(created);
        Assert.AreNotEqual(Guid.Empty, created!.Id);

        var getResp = await client.GetAsync($"api/documents/{created.Id}");
        Assert.AreEqual(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await getResp.Content.ReadFromJsonAsync<DocumentResponse>();
        Assert.AreEqual(created.Id, fetched!.Id);
        Assert.AreEqual(create.Name, fetched.Name);
    }

    [TestMethod]
    public async Task IdempotencyKey_ReplaysSameResponse()
    {
        if (string.IsNullOrEmpty(BaseUrl)) return;
        using var client = NewClient();

        var key = "smoke-" + Guid.NewGuid();
        var create = new CreateDocumentRequest
        {
            Name = "idemp.txt",
            ContentType = "text/plain",
            Content = "v1",
        };

        async Task<HttpResponseMessage> Post()
        {
            var json = JsonSerializer.Serialize(create);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var msg = new HttpRequestMessage(HttpMethod.Post, "api/documents") { Content = content };
            msg.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(msg);
        }

        var first = await Post();
        if (first.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            await Task.Delay(2000);
            first = await Post();
        }
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
        var firstBody = await first.Content.ReadAsStringAsync();

        var second = await Post();
        Assert.AreEqual(HttpStatusCode.Created, second.StatusCode);
        var secondBody = await second.Content.ReadAsStringAsync();

        Assert.AreEqual(firstBody, secondBody, "Idempotent replay should return identical body");
    }
}
