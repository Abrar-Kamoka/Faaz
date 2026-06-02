namespace Faaz.Gateway.Middleware;

/// <summary>Blocks any request whose path contains /internal/ from reaching the gateway.</summary>
public sealed class BlockInternalRoutesMiddleware
{
    private readonly RequestDelegate _next;

    public BlockInternalRoutesMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Value?.Contains("/internal/", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }

        return _next(context);
    }
}
