namespace JobFitAI.Web.Services;

/// <summary>Thrown when the AI analysis can't be completed (API down, rate limited,
/// bad key, unexpected reply). The caller decides what to do, e.g. fall back to the keyword check.</summary>
public sealed class AnalysisUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);
