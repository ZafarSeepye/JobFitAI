using System.Net;

namespace JobFitAI.Tests;

/// <summary>Stands in for the internet: returns a canned response and remembers the request,
/// so the Claude code can be tested without calling the real API (free, fast, offline).</summary>
internal sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
    }
}
