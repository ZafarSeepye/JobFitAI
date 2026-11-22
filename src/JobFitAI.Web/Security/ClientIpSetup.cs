using System.Net;

namespace JobFitAI.Web.Security;

/// <summary>Finds the visitor's real IP address when the app is hosted behind Cloudflare (as on Render).</summary>
/// <remarks>
/// On Render, every request passes through Cloudflare and then Render's own proxy, so the address the
/// app sees directly is an internal Render address shared by everyone. Cloudflare puts the visitor's
/// real IP in its own headers (CF-Connecting-IP, True-Client-IP) and overwrites any value a visitor sends,
/// so those headers can be trusted there. X-Forwarded-For can't: Render keeps whatever the visitor sent.
///
/// Only switch this on (Hosting:ClientIpHeaders) where Cloudflare really sits in front of the app.
/// Anywhere else, a visitor could send these headers themselves and dodge the rate limit.
/// </remarks>
public static class ClientIpSetup
{
    /// <summary>Key under which the original, unmodified connection address is kept (for the temporary check page).</summary>
    public const string OriginalRemoteIpKey = "OriginalRemoteIp";

    public static IApplicationBuilder UseClientIpFromHeaders(this IApplicationBuilder app, IReadOnlyList<string> headerNames)
    {
        return app.Use(async (context, next) =>
        {
            context.Items[OriginalRemoteIpKey] = context.Connection.RemoteIpAddress?.ToString();

            foreach (var name in headerNames)
            {
                var value = context.Request.Headers[name].ToString().Trim();
                if (IPAddress.TryParse(value, out var ip))
                {
                    context.Connection.RemoteIpAddress = ip;
                    break;
                }
            }

            await next();
        });
    }

    /// <summary>Reads "True-Client-IP,CF-Connecting-IP" style configuration into a clean list.</summary>
    public static string[] ParseHeaderList(string? configured) =>
        string.IsNullOrWhiteSpace(configured)
            ? []
            : configured.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
