namespace JobFitAI.Web.Security;

/// <summary>Limits on how many analyses can run, read from the "RateLimits" configuration section.</summary>
public sealed class RateLimitOptions
{
    public const string Section = "RateLimits";

    /// <summary>Analyses one visitor (IP address) may run per day.</summary>
    public int PerVisitorPerDay { get; set; } = 5;

    /// <summary>Analyses everyone together may run per day: a safety net for the API budget.</summary>
    public int GlobalPerDay { get; set; } = 100;
}
