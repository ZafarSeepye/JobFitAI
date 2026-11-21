using System.Text.RegularExpressions;
using JobFitAI.Web.Models;

namespace JobFitAI.Web.Services;

/// <summary>A free, offline analyser based on keyword matching.</summary>
/// <remarks>Used while building the app (no API costs) and as a fallback.
/// It finds known skills in the job advert, then checks which ones the CV mentions.
/// It is deliberately simple: it cannot judge experience or context the way Claude can.</remarks>
public sealed class MockFitAnalyser : IFitAnalyser
{
    // Display name → patterns that count as a mention. Matching is case-insensitive.
    private static readonly (string Name, string[] Patterns)[] Skills =
    [
        ("C#", [@"c#", @"c sharp"]),
        (".NET", [@"\.net\b", @"dotnet"]),
        ("ASP.NET Core", [@"asp\.net"]),
        ("SQL and relational databases", [@"\bsql\b", @"database", @"t-sql"]),
        ("Python", [@"\bpython\b"]),
        ("Java", [@"\bjava\b"]),
        ("JavaScript", [@"javascript", @"\bjs\b"]),
        ("TypeScript", [@"typescript"]),
        ("HTML and CSS", [@"\bhtml", @"\bcss\b"]),
        ("React", [@"\breact\b"]),
        ("Angular", [@"\bangular\b"]),
        ("Git and version control", [@"\bgit\b", @"github", @"version control"]),
        ("Unit testing", [@"unit test", @"xunit", @"nunit", @"mstest", @"\btdd\b"]),
        ("Cloud (Azure or AWS)", [@"\bazure\b", @"\baws\b", @"\bcloud\b"]),
        ("REST APIs", [@"\bapi\b", @"\bapis\b", @"\brest\b"]),
        ("Agile ways of working", [@"\bagile\b", @"\bscrum\b", @"stand-?ups?", @"\bsprints?\b"]),
        ("Docker and containers", [@"docker", @"container"]),
        ("Communication skills", [@"communicat"]),
        ("Problem solving", [@"problem[- ]solv"]),
        ("Customer or client support", [@"customer service", @"customer support", @"client support", @"help ?desk"]),
    ];

    public Task<FitResult> AnalyseAsync(string jobAd, string cv, CancellationToken cancellationToken = default)
    {
        var required = Skills.Where(s => Mentions(jobAd, s.Patterns)).ToList();

        var matches = required
            .Where(s => Mentions(cv, s.Patterns))
            .Select(s => new RequirementItem(s.Name, $"The CV mentions {s.Name}. Back it up with a project or result."))
            .ToList();

        var gaps = required
            .Where(s => !Mentions(cv, s.Patterns))
            .Select(s => new RequirementItem(s.Name, $"The job asks for {s.Name}, but the CV doesn't mention it. Add it if you have it, or show how you'd learn it."))
            .ToList();

        // No recognised skills in the advert: we can't score it, so stay neutral.
        var score = required.Count == 0 ? 50 : (int)Math.Round(100.0 * matches.Count / required.Count);
        var verdict = Verdicts.FromScore(score);

        var headline = required.Count == 0
            ? "No familiar skills were found in this advert, so this quick check can't judge the fit."
            : $"The CV covers {matches.Count} of the {required.Count} skills this advert mentions.";

        var summary = matches.Count == 0
            ? "Add a short profile that names the skills this role asks for, with one project that proves each one."
            : $"Candidate with hands-on experience in {JoinNatural(matches.Select(m => m.Requirement).Take(4).ToList())}, looking to apply these skills in this role.";

        var tips = new List<string>
        {
            "Mirror the advert's wording for skills you really have, so screening software finds them.",
            gaps.Count > 0 ? $"Address the biggest gap first: {gaps[0].Requirement}." : "Lead with your strongest matching project.",
            "This is the quick keyword check. The full AI analysis gives deeper, context-aware feedback."
        };

        var result = new FitResult(score, verdict, headline, matches, gaps, summary, tips, Source: "mock");
        return Task.FromResult(result);
    }

    private static bool Mentions(string text, string[] patterns) =>
        patterns.Any(p => Regex.IsMatch(text, p, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

    private static string JoinNatural(IReadOnlyList<string> items) => items.Count switch
    {
        0 => "",
        1 => items[0],
        _ => string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1]
    };
}
