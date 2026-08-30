using Faaz.BuildingBlocks.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Faaz.BuildingBlocks.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFaazMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();
        return app;
    }
}
