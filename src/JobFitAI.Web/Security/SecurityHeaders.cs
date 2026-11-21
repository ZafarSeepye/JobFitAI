namespace JobFitAI.Web.Security;

/// <summary>Adds HTTP security headers to every response.</summary>
public static class SecurityHeaders
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app, bool isDevelopment)
    {
        // Content Security Policy: only load scripts, styles, fonts and data from this site.
        // An injected <script> from anywhere else is blocked by the browser.
        // In development, dotnet watch's live reload uses a WebSocket, so allow that locally.
        var connect = isDevelopment ? "'self' ws://localhost:* wss://localhost:*" : "'self'";
        var csp = string.Join("; ",
            "default-src 'self'",
            "script-src 'self'",
            "style-src 'self'",
            "font-src 'self'",
            "img-src 'self' data:",
            $"connect-src {connect}",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");

        return app.Use(async (context, next) =>
        {
            var h = context.Response.Headers;
            h.ContentSecurityPolicy = csp;
            h.XContentTypeOptions = "nosniff";                 // don't guess file types
            h.XFrameOptions = "DENY";                          // can't be put in an iframe (clickjacking)
            h["Referrer-Policy"] = "no-referrer";              // don't leak the URL to other sites
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            h["Cross-Origin-Opener-Policy"] = "same-origin";
            await next();
        });
    }
}
