using JobFitAI.Web.Models;

namespace JobFitAI.Web.Services;

/// <summary>Anything that can compare a job advert with a CV.</summary>
/// <remarks>The endpoint depends on this interface, not on a concrete class.
/// Step 3 plugs in <see cref="MockFitAnalyser"/>; Step 5 adds a Claude-powered
/// analyser without changing the endpoint (dependency injection).</remarks>
public interface IFitAnalyser
{
    Task<FitResult> AnalyseAsync(string jobAd, string cv, CancellationToken cancellationToken = default);
}
