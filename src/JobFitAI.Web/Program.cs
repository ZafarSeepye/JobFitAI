// JobFit AI - entry point.
using JobFitAI.Web.Models;
using JobFitAI.Web.Services;
using JobFitAI.Web.Validation;

var builder = WebApplication.CreateBuilder(args);

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

// Return errors in the standard "problem details" JSON format (RFC 9457).
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!claude.HasApiKey)
    app.Logger.LogWarning("No Claude API key configured. Running with the keyword check only.");

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
// 200 OK + FitResult when valid; 400 Bad Request + field errors when not.
app.MapPost("/api/analyse", async (AnalyseRequest? request, IFitAnalyser analyser, CancellationToken ct) =>
{
    var errors = AnalyseRequestValidator.Validate(request);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors);

    var result = await analyser.AnalyseAsync(request!.JobAd!.Trim(), request.Cv!.Trim(), ct);
    return Results.Ok(result);
})
.Accepts<AnalyseRequest>("application/json")
.Produces<FitResult>(StatusCodes.Status200OK)
.ProducesValidationProblem();

app.Run();

// Lets the test project (Step 7) start the app in memory.
public partial class Program { }
