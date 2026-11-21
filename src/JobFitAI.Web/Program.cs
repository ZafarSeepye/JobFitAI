// JobFit AI - entry point.
// Step 1: serve the front end from wwwroot and expose a health check.
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Serve wwwroot/index.html when someone opens the site root,
// then serve any other static file (CSS, JS, fonts) from wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

// A simple endpoint to confirm the API is running.
// Hosting platforms (and we) can call it to check the app is alive.
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    app = "JobFit AI",
    time = DateTime.UtcNow
}));

app.Run();
