using Microsoft.Net.Http.Headers;

namespace SchoolManagement.Api.Infrastructure;

/// <summary>
/// Caching rules for the Angular build when the API serves it (the Azure deployment) –
/// the same rules as frontend/nginx.conf.
/// </summary>
public static class WebAppFiles
{
    public static readonly StaticFileOptions Options = new()
    {
        OnPrepareResponse = context =>
        {
            // index.html points to the current bundles, so the browser must always check for a new one.
            // The bundle files have a content hash in their name and never change.
            context.Context.Response.Headers[HeaderNames.CacheControl] =
                context.File.Name == "index.html" ? "no-cache" : "public, max-age=31536000, immutable";
        }
    };
}
