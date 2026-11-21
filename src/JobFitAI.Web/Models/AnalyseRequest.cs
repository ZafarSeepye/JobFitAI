namespace JobFitAI.Web.Models;

/// <summary>What the browser sends to POST /api/analyse.</summary>
/// <remarks>Both properties are nullable because the request comes from outside
/// and may be missing fields. The validator decides whether it is usable.</remarks>
public sealed record AnalyseRequest(string? JobAd, string? Cv);
