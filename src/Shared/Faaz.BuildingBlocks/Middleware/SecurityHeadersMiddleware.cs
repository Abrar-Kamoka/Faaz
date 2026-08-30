using Microsoft.AspNetCore.Http;

namespace Faaz.BuildingBlocks.Middleware;

/// <summary>
/// Adds baseline security headers to every response. These are pure JSON APIs (no HTML views besides
/// Swagger/Hangfire in dev), so this deliberately skips a Content-Security-Policy — nosniff/frame-options/
/// referrer-policy are the headers that matter for a JSON API and carry no risk of breaking anything.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
