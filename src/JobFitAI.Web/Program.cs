// JobFit AI - entry point.
using JobFitAI.Web.Models;
using JobFitAI.Web.Security;
using JobFitAI.Web.Services;
using JobFitAI.Web.Validation;

var builder = WebApplication.CreateBuilder(args);

// ----- Request size limit -----
// Two 8 000-character documents are well under 100 KB. Anything bigger is rejected with 413
// before it is even read, so nobody can send a huge payload.
builder.WebHost.ConfigureKestrel(k =>
{
    k.Limits.MaxRequestBodySize = 100_000;
    k.AddServerHeader = false;            // don't advertise which web server we run
});

// ----- Analysis services -----
// Claude settings come from the "Claude" section of configuration.
// The API key is stored outside the code: dotnet user-secrets locally, an environment variable in production.
builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection(ClaudeOptions.Section));
var claude = builder.Configuration.GetSection(ClaudeOptions.Section).Get<ClaudeOptions>() ?? new ClaudeOptions();

builder.Services.AddSingleton<MockFitAnalyser>();

// A typed HttpClient for the Claude API (HttpClientFactory manages the connections).
builder.Services.AddHttpClient<ClaudeFitAnalyser>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.Timeout = TimeSpan.FromSeconds(claude.TimeoutSeconds);
});

// Use Claude (with the keyword check as a fallback) when a key is configured; otherwise just the keyword check.
if (claude.HasApiKey)
    builder.Services.AddScoped<IFitAnalyser, FallbackFitAnalyser>();
else
    builder.Services.AddSingleton<IFitAnalyser>(sp => sp.GetRequiredService<MockFitAnalyser>());

// ----- Protection -----
var limits = builder.Configuration.GetSection(RateLimitOptions.Section).Get<RateLimitOptions>() ?? new RateLimitOptions();
var behindProxy = builder.Configuration.GetValue<bool>("Hosting:BehindProxy");   // true only on the hosting platform
builder.Services.AddAnalysisRateLimits(limits, behindProxy);

// Errors use the standard "problem details" JSON format (RFC 9457), without internal details in production.
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!claude.HasApiKey)
    app.Logger.LogWarning("No Claude API key configured. Running with the keyword check only.");

// Order matters: each middleware runs in turn for every request.
if (behindProxy)
    app.UseForwardedHeaders();                // real visitor IP and scheme, only when behind a trusted proxy
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();                // unexpected errors → generic 500, no stack traces
    app.UseHsts();                            // tell browsers to only use HTTPS for this site
}
app.UseStatusCodePages();                     // empty error responses get a problem-details body
app.UseSecurityHeaders(app.Environment.IsDevelopment());
app.UseRateLimiter();

// Serve wwwroot/index.html for "/", then any other static file (CSS, JS, fonts).
app.UseDefaultFiles();
app.UseStaticFiles();

// Health check: confirms the app is running and which analyser is active.
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    app = "JobFit AI",
    analyser = claude.HasApiKey ? "claude" : "keyword",
    time = DateTime.UtcNow
}));

// Analyse a job advert against a CV.
// 200 OK + FitResult when valid; 400 Bad Request + field errors when not; 429 when over the daily limit.
app.MapPost(RateLimitingSetup.AnalysePath, async (AnalyseRequest? request, IFitAnalyser analyser, CancellationToken ct) =>
{
    var errors = AnalyseRequestValidator.Validate(request);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var result = await analyser.AnalyseAsync(request!.JobAd!.Trim(), request.Cv!.Trim(), ct);
    return Results.Ok(result);
})
.Accepts<AnalyseRequest>("application/json")
.Produces<FitResult>(StatusCodes.Status200OK)
.ProducesValidationProblem()
.Produces(StatusCodes.Status429TooManyRequests);

app.Run();

// Lets the test project (Step 7) start the app in memory.
public partial class Program { }
