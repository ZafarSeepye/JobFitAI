using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

namespace JobFitAI.Web.Security;

/// <summary>Rate limiting for POST /api/analyse: a per-visitor daily limit plus a global daily cap.</summary>
/// <remarks>Counters live in memory, so they reset if the app restarts. That's acceptable for a
/// portfolio app; a larger app would keep them in a shared store such as Redis.</remarks>
public static class RateLimitingSetup
{
    public const string AnalysePath = "/api/analyse";

    public static IServiceCollection AddAnalysisRateLimits(this IServiceCollection services, RateLimitOptions limits, bool behindProxy)
    {
        // Behind a hosting platform's proxy, the visitor's real IP arrives in X-Forwarded-For.
        // Only trust that header when the app really is behind a proxy (Hosting:BehindProxy = true).
        // Otherwise anyone could send a fake X-Forwarded-For with every request and dodge the per-visitor limit.
        // ForwardLimit = 1 reads only the address added by the nearest proxy, which visitors can't fake.
        if (behindProxy)
        {
            services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.ForwardLimit = 1;
                o.KnownIPNetworks.Clear();
                o.KnownProxies.Clear();
            });
        }

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Two limiters chained together: a request must pass both.
            // Requests to anything other than the analyse endpoint are never limited.
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    IsAnalyse(ctx)
                        ? RateLimitPartition.GetFixedWindowLimiter("visitor:" + VisitorKey(ctx), _ => Daily(limits.PerVisitorPerDay))
                        : RateLimitPartition.GetNoLimiter("unlimited")),
                PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                    IsAnalyse(ctx)
                        ? RateLimitPartition.GetFixedWindowLimiter("global", _ => Daily(limits.GlobalPerDay))
                        : RateLimitPartition.GetNoLimiter("unlimited")));

            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Daily limit reached",
                    Detail = $"You've used today's {limits.PerVisitorPerDay} free analyses, or the site has reached its daily limit. Try again tomorrow."
                };
                await context.HttpContext.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", ct);
            };
        });

        return services;
    }

    private static bool IsAnalyse(HttpContext ctx) =>
        HttpMethods.IsPost(ctx.Request.Method) &&
        ctx.Request.Path.Equals(AnalysePath, StringComparison.OrdinalIgnoreCase);

    private static string VisitorKey(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString() ?? IPAddress.None.ToString();

    private static FixedWindowRateLimiterOptions Daily(int permits) => new()
    {
        PermitLimit = permits,
        Window = TimeSpan.FromDays(1),
        QueueLimit = 0,                 // don't queue: reject straight away
        AutoReplenishment = true
    };
}
