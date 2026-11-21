using JobFitAI.Web.Models;

namespace JobFitAI.Web.Services;

/// <summary>Tries Claude first; if the AI analysis is unavailable, answers with the keyword check instead.</summary>
/// <remarks>Users always get a result. The "source" field tells the page which one it was,
/// so keyword results are clearly labelled.</remarks>
public sealed class FallbackFitAnalyser(
    ClaudeFitAnalyser primary,
    MockFitAnalyser fallback,
    ILogger<FallbackFitAnalyser> logger) : IFitAnalyser
{
    public async Task<FitResult> AnalyseAsync(string jobAd, string cv, CancellationToken cancellationToken = default)
    {
        try
        {
            return await primary.AnalyseAsync(jobAd, cv, cancellationToken);
        }
        catch (AnalysisUnavailableException ex)
        {
            logger.LogWarning(ex, "AI analysis unavailable ({Reason}). Using the keyword check.", ex.Message);
            return await fallback.AnalyseAsync(jobAd, cv, cancellationToken);
        }
    }
}
