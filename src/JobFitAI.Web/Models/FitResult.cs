namespace JobFitAI.Web.Models;

/// <summary>The analysis returned to the browser. Serialised as camelCase JSON,
/// matching the contract defined in wwwroot/js/sample.js.</summary>
public sealed record FitResult(
    int Score,
    string Verdict,
    string Headline,
    IReadOnlyList<RequirementItem> Matches,
    IReadOnlyList<RequirementItem> Gaps,
    string Summary,
    IReadOnlyList<string> Tips,
    string Source);

/// <summary>One requirement from the job advert, with either the CV evidence
/// (for a match) or a suggestion for closing it (for a gap).</summary>
public sealed record RequirementItem(string Requirement, string Evidence);

/// <summary>The three possible verdicts, kept in one place so the rules and the UI agree.</summary>
public static class Verdicts
{
    public const string Apply = "apply";
    public const string Stretch = "stretch";
    public const string Skip = "skip";

    /// <summary>70 and above: apply. 45 to 69: a stretch. Below 45: skip.</summary>
    public static string FromScore(int score) =>
        score >= 70 ? Apply : score >= 45 ? Stretch : Skip;
}
