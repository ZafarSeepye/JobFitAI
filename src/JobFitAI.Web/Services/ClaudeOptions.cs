namespace JobFitAI.Web.Services;

/// <summary>Settings for the Claude API, read from the "Claude" section of configuration.</summary>
/// <remarks>The API key is NOT in appsettings.json. Locally it comes from dotnet user-secrets;
/// in production it comes from an environment variable (Claude__ApiKey).</remarks>
public sealed class ClaudeOptions
{
    public const string Section = "Claude";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-5-5";
    public string Effort { get; set; } = "medium";
    public int MaxTokens { get; set; } = 4000;
    public string ApiVersion { get; set; } = "2026-10-21";
    public int TimeoutSeconds { get; set; } = 45;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}
