using System.Net;
using FluentAssertions;

namespace SchoolManagement.IntegrationTests.Infrastructure;

/// <summary>The Azure setup: the API also serves the Angular build from wwwroot.</summary>
public sealed class WebAppHostingTests : IDisposable
{
    private readonly string _webRoot = Path.Combine(Path.GetTempPath(), $"api-tests-wwwroot-{Guid.NewGuid():N}");
    private readonly ApiFactory _factory = new();

    public WebAppHostingTests()
    {
        Directory.CreateDirectory(_webRoot);
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<app-root></app-root>");
        File.WriteAllText(Path.Combine(_webRoot, "main-ABC123.js"), "console.log('app');");
        _factory.Settings["webroot"] = _webRoot;
    }

    [Fact]
    public async Task An_Angular_route_returns_index_html_without_login()
    {
        var response = await _factory.CreateClient().GetAsync("/students/5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("<app-root>");
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
    }

    [Fact]
    public async Task Bundle_files_are_served_and_cached()
    {
        var response = await _factory.CreateClient().GetAsync("/main-ABC123.js");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.MaxAge.Should().Be(TimeSpan.FromDays(365));
    }

    [Fact]
    public async Task An_unknown_api_path_is_still_a_404()
    {
        var response = await _factory.CreateClient().GetAsync("/api/no-such-endpoint");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task The_api_still_requires_a_login()
    {
        var response = await _factory.CreateClient().GetAsync("/api/students");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _factory.Dispose();
        Directory.Delete(_webRoot, recursive: true);
    }
}
