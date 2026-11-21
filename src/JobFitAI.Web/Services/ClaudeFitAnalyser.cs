using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using JobFitAI.Web.Models;
using Microsoft.Extensions.Options;

namespace JobFitAI.Web.Services;

/// <summary>Analyses the fit by calling the Claude Messages API with structured outputs.</summary>
public sealed class ClaudeFitAnalyser(HttpClient http, IOptions<ClaudeOptions> options, ILogger<ClaudeFitAnalyser> logger)
    : IFitAnalyser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ClaudeOptions _options = options.Value;

    public async Task<FitResult> AnalyseAsync(string jobAd, string cv, CancellationToken cancellationToken = default)
    {
        if (!_options.HasApiKey)
            throw new AnalysisUnavailableException("No Claude API key is configured.");

        var body = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            system = ClaudePrompt.System,
            messages = new[] { new { role = "user", content = ClaudePrompt.UserMessage(jobAd, cv) } },
            output_config = new
            {
                effort = _options.Effort,
                format = new { type = "json_schema", schema = ClaudePrompt.Schema }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", _options.ApiVersion);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new AnalysisUnavailableException("Couldn't reach the Claude API.", ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // Log the status and Anthropic's error type, never the API key or the user's documents.
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Claude API returned {Status}: {Error}", (int)response.StatusCode, Truncate(errorText, 300));
                throw new AnalysisUnavailableException(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "The Claude API key is invalid.",
                    HttpStatusCode.TooManyRequests => "The Claude API rate limit was reached.",
                    (HttpStatusCode)529 => "The Claude API is overloaded.",
                    _ => $"The Claude API returned {(int)response.StatusCode}."
                });
            }

            var message = await response.Content.ReadFromJsonAsync<ClaudeMessage>(Json, cancellationToken)
                ?? throw new AnalysisUnavailableException("The Claude API returned an empty response.");

            logger.LogInformation("Claude analysis done: {Input} input tokens, {Output} output tokens, stop: {Stop}",
                message.Usage?.InputTokens, message.Usage?.OutputTokens, message.StopReason);

            return ToFitResult(message);
        }
    }

    /// <summary>Turns Claude's reply into our FitResult. Public and static so it can be unit tested.</summary>
    public static FitResult ToFitResult(ClaudeMessage message)
    {
        if (message.StopReason is "refusal" or "max_tokens")
            throw new AnalysisUnavailableException($"Claude stopped early ({message.StopReason}).");

        // With thinking enabled the content can hold several blocks; the JSON answer is in the text block.
        var text = message.Content?.LastOrDefault(b => b.Type == "text")?.Text
            ?? throw new AnalysisUnavailableException("Claude's reply had no text block.");

        ClaudeFitPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<ClaudeFitPayload>(text, Json)
                ?? throw new AnalysisUnavailableException("Claude's reply was empty.");
        }
        catch (JsonException ex)
        {
            throw new AnalysisUnavailableException("Claude's reply wasn't valid JSON.", ex);
        }

        // The schema can't enforce a range, so clamp here. The verdict is derived from the score
        // by our own rule, so the stamp and the score can never disagree.
        var score = Math.Clamp(payload.Score, 0, 100);

        return new FitResult(
            Score: score,
            Verdict: Verdicts.FromScore(score),
            Headline: payload.Headline,
            Matches: payload.Matches ?? [],
            Gaps: payload.Gaps ?? [],
            Summary: payload.Summary,
            Tips: payload.Tips ?? [],
            Source: "claude");
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}

// ----- Shapes of the Claude API reply (only the fields we use) -----

public sealed record ClaudeMessage(
    [property: JsonPropertyName("content")] List<ClaudeContentBlock>? Content,
    [property: JsonPropertyName("stop_reason")] string? StopReason,
    [property: JsonPropertyName("usage")] ClaudeUsage? Usage);

public sealed record ClaudeContentBlock(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string? Text);

public sealed record ClaudeUsage(
    [property: JsonPropertyName("input_tokens")] int InputTokens,
    [property: JsonPropertyName("output_tokens")] int OutputTokens);

/// <summary>The JSON object Claude returns, matching ClaudePrompt.Schema.</summary>
public sealed record ClaudeFitPayload(
    int Score,
    string Headline,
    List<RequirementItem>? Matches,
    List<RequirementItem>? Gaps,
    string Summary,
    List<string>? Tips);
