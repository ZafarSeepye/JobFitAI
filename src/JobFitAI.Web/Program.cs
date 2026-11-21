// JobFit AI - entry point.
using JobFitAI.Web.Models;
using JobFitAI.Web.Services;
using JobFitAI.Web.Validation;

var builder = WebApplication.CreateBuilder(args);

// Dependency injection: whenever something asks for an IFitAnalyser, give it the mock.
// Step 5 swaps this for the Claude analyser without touching the endpoint below.
builder.Services.AddSingleton<IFitAnalyser, MockFitAnalyser>();

// Return errors in the standard "problem details" JSON format (RFC 9457).
builder.Services.AddProblemDetails();

var app = builder.Build();

// Serve wwwroot/index.html for "/", then any other static file (CSS, JS, fonts).
app.UseDefaultFiles();
app.UseStaticFiles();

// Health check: confirms the app is running.
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    app = "JobFit AI",
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
