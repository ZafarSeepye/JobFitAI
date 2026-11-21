using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JobFitAI.Tests;

/// <summary>Starts the whole app in memory, so these tests go through the real
/// middleware, validation, rate limiting and JSON, exactly like a browser would.</summary>
internal sealed class JobFitAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" is not "Development", so user-secrets (and your real API key) are never loaded.
        // The tests always use the free keyword check and can never spend money.
        builder.UseEnvironment("Testing");
        builder.UseSetting("Claude:ApiKey", "");
    }
}

[TestClass]
public sealed class ApiTests
{
    private static readonly object ValidBody = new { jobAd = TestData.JobAd, cv = TestData.Cv };

    [TestMethod]
    public async Task Health_check_returns_ok()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("keyword", json.RootElement.GetProperty("analyser").GetString());
    }

    [TestMethod]
    public async Task Valid_request_returns_a_result_in_the_agreed_shape()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analyse", ValidBody);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        foreach (var field in new[] { "score", "verdict", "headline", "matches", "gaps", "summary", "tips", "source" })
            Assert.IsTrue(root.TryGetProperty(field, out _), $"Missing field: {field}");
        Assert.AreEqual("mock", root.GetProperty("source").GetString());
    }

    [TestMethod]
    public async Task Empty_request_returns_400_with_field_errors()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/api/analyse", new { jobAd = "", cv = "" });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = json.RootElement.GetProperty("errors");
        Assert.IsTrue(errors.TryGetProperty("jobAd", out _));
        Assert.IsTrue(errors.TryGetProperty("cv", out _));
    }

    [TestMethod]
    public async Task Invalid_json_returns_400_without_internal_details()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        var response = await client.PostAsync("/api/analyse",
            new StringContent("this is not json", System.Text.Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("Exception"), "Internal exception details leaked");
    }

    [TestMethod]
    public async Task Client_cannot_fake_the_score()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        // Field tampering: extra fields are ignored; the score always comes from the server.
        var response = await client.PostAsJsonAsync("/api/analyse",
            new { jobAd = TestData.JobAd, cv = "I enjoy painting and hiking, and I like long walks in the mountains. " + TestData.OfLength(200), score = 100, verdict = "apply" });

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual(0, json.RootElement.GetProperty("score").GetInt32());
        Assert.AreEqual("skip", json.RootElement.GetProperty("verdict").GetString());
    }

    [TestMethod]
    public async Task Sixth_analysis_in_a_day_is_rate_limited()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        for (var i = 1; i <= 5; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/analyse", ValidBody);
            Assert.AreEqual(HttpStatusCode.OK, ok.StatusCode, $"Request {i} should be allowed");
        }

        var limited = await client.PostAsJsonAsync("/api/analyse", ValidBody);

        Assert.AreEqual(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.IsTrue(limited.Headers.Contains("Retry-After"));
    }

    [TestMethod]
    public async Task Fake_forwarded_ip_cannot_dodge_the_rate_limit()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        // An attacker sends a different fake IP with every request.
        HttpResponseMessage? last = null;
        for (var i = 1; i <= 6; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/analyse") { Content = JsonContent.Create(ValidBody) };
            request.Headers.Add("X-Forwarded-For", $"10.0.0.{i}");
            last = await client.SendAsync(request);
        }

        Assert.AreEqual(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [TestMethod]
    public async Task Rate_limit_does_not_block_the_health_check()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();
        for (var i = 0; i < 6; i++)
            await client.PostAsJsonAsync("/api/analyse", ValidBody);

        var health = await client.GetAsync("/api/health");

        Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
    }

    [TestMethod]
    public async Task Security_headers_are_set()
    {
        await using var app = new JobFitAppFactory();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/health");

        foreach (var header in new[] { "Content-Security-Policy", "X-Content-Type-Options", "X-Frame-Options", "Referrer-Policy" })
            Assert.IsTrue(response.Headers.Contains(header), $"Missing header: {header}");
        Assert.IsFalse(response.Headers.Contains("Server"), "Server header should be hidden");
        Assert.IsTrue(response.Headers.GetValues("Content-Security-Policy").Single().Contains("frame-ancestors 'none'"));
    }
}
