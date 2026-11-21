using System.Net;
using System.Text.Json;
using JobFitAI.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobFitAI.Tests;

[TestClass]
public sealed class ClaudeFitAnalyserTests
{
    /// <summary>A realistic Claude reply: a thinking block, then the JSON answer in a text block.</summary>
    private static string ClaudeReply(int score = 82, string stopReason = "end_turn")
    {
        var answer = JsonSerializer.Serialize(new
        {
            score,
            headline = "Strong fit for the core .NET work.",
            matches = new[] { new { requirement = "C# and .NET", evidence = "Built the Clinic Booking System in ASP.NET Core." } },
            gaps = new[] { new { requirement = "Cloud", evidence = "Deploy one project to Azure." } },
            summary = "Final-year student who builds ASP.NET Core apps.",
            tips = new[] { "Lead with the booking system.", "Name the test framework.", "Link the repo." }
        });

        return JsonSerializer.Serialize(new
        {
            id = "msg_test",
            type = "message",
            role = "assistant",
            content = new object[]
            {
                new { type = "thinking", thinking = "Comparing requirements...", signature = "sig" },
                new { type = "text", text = answer }
            },
            stop_reason = stopReason,
            usage = new { input_tokens = 1200, output_tokens = 400 }
        });
    }

    private static ClaudeFitAnalyser CreateAnalyser(FakeHttpHandler handler, string? apiKey = "test-key")
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com/") };
        var options = Options.Create(new ClaudeOptions { ApiKey = apiKey });
        return new ClaudeFitAnalyser(http, options, NullLogger<ClaudeFitAnalyser>.Instance);
    }

    [TestMethod]
    public async Task Reads_the_json_answer_from_claudes_reply()
    {
        var analyser = CreateAnalyser(new FakeHttpHandler(HttpStatusCode.OK, ClaudeReply(score: 82)));

        var result = await analyser.AnalyseAsync(TestData.JobAd, TestData.Cv);

        Assert.AreEqual(82, result.Score);
        Assert.AreEqual("apply", result.Verdict);
        Assert.AreEqual("claude", result.Source);
        Assert.AreEqual("C# and .NET", result.Matches[0].Requirement);
        Assert.AreEqual(3, result.Tips.Count);
    }

    [TestMethod]
    public async Task Sends_the_key_version_and_schema_to_the_api()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, ClaudeReply());
        var analyser = CreateAnalyser(handler);

        await analyser.AnalyseAsync(TestData.JobAd, TestData.Cv);

        var request = handler.LastRequest!;
        Assert.AreEqual(HttpMethod.Post, request.Method);
        Assert.AreEqual("https://api.anthropic.com/v1/messages", request.RequestUri!.ToString());
        Assert.AreEqual("test-key", request.Headers.GetValues("x-api-key").Single());
        Assert.IsTrue(request.Headers.Contains("anthropic-version"));

        using var body = JsonDocument.Parse(handler.LastRequestBody!);
        var format = body.RootElement.GetProperty("output_config").GetProperty("format");
        Assert.AreEqual("json_schema", format.GetProperty("type").GetString());
        // The documents are wrapped in tags so Claude treats them as data (prompt-injection defence).
        var userMessage = body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString()!;
        Assert.IsTrue(userMessage.Contains("<job_advert>"));
        Assert.IsTrue(userMessage.Contains("<cv>"));
    }

    [TestMethod]
    [DataRow(150, 100)]
    [DataRow(-20, 0)]
    public async Task Out_of_range_scores_are_clamped(int claudeScore, int expected)
    {
        var analyser = CreateAnalyser(new FakeHttpHandler(HttpStatusCode.OK, ClaudeReply(score: claudeScore)));

        var result = await analyser.AnalyseAsync(TestData.JobAd, TestData.Cv);

        Assert.AreEqual(expected, result.Score);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Unauthorized)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    [DataRow(HttpStatusCode.InternalServerError)]
    [DataRow((HttpStatusCode)529)]
    public async Task Api_errors_become_analysis_unavailable(HttpStatusCode status)
    {
        var analyser = CreateAnalyser(new FakeHttpHandler(status, """{"type":"error","error":{"type":"api_error","message":"x"}}"""));

        await Assert.ThrowsExactlyAsync<AnalysisUnavailableException>(
            () => analyser.AnalyseAsync(TestData.JobAd, TestData.Cv));
    }

    [TestMethod]
    [DataRow("max_tokens")]
    [DataRow("refusal")]
    public async Task Incomplete_replies_are_rejected(string stopReason)
    {
        var analyser = CreateAnalyser(new FakeHttpHandler(HttpStatusCode.OK, ClaudeReply(stopReason: stopReason)));

        await Assert.ThrowsExactlyAsync<AnalysisUnavailableException>(
            () => analyser.AnalyseAsync(TestData.JobAd, TestData.Cv));
    }

    [TestMethod]
    public async Task Missing_api_key_never_calls_the_api()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, ClaudeReply());
        var analyser = CreateAnalyser(handler, apiKey: null);

        await Assert.ThrowsExactlyAsync<AnalysisUnavailableException>(
            () => analyser.AnalyseAsync(TestData.JobAd, TestData.Cv));
        Assert.IsNull(handler.LastRequest);
    }

    [TestMethod]
    public async Task Falls_back_to_the_keyword_check_when_claude_fails()
    {
        var claude = CreateAnalyser(new FakeHttpHandler(HttpStatusCode.InternalServerError, "{}"));
        var fallback = new FallbackFitAnalyser(claude, new MockFitAnalyser(), NullLogger<FallbackFitAnalyser>.Instance);

        var result = await fallback.AnalyseAsync(TestData.JobAd, TestData.Cv);

        Assert.AreEqual("mock", result.Source);
        Assert.IsTrue(result.Matches.Count > 0);
    }
}
