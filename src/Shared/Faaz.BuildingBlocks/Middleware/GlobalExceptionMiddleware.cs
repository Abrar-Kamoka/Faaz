using System.Diagnostics;
using System.Text.Json;
using Faaz.SharedKernel.Exceptions;
using Faaz.SharedKernel.Results;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Faaz.BuildingBlocks.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId       = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? traceId;

        int    status;
        string message;
        object? payload   = null;
        string? detail    = null;
        string? stackTrace = null;
        Dictionary<string, string[]>? errors = null;

        switch (exception)
        {
            case ValidationException ex:
                status  = 400;
                message = "One or more validation errors occurred.";
                errors  = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case ConflictException ex:
                status  = 409;
                message = ex.Message;
                payload = ex.Payload;
                break;

            case NotFoundException ex:
                status  = 404;
                message = ex.Message;
                break;

            case ForbiddenException ex:
                status  = 403;
                message = ex.Message;
                break;

            case UnauthorizedAccessException:
                status  = 401;
                message = "Unauthorized.";
                break;

            case BusinessRuleException ex:
                status  = 422;
                message = ex.Message;
                break;

            default:
                status  = 500;
                message = _env.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred.";
                if (_env.IsDevelopment())
                {
                    detail     = exception.InnerException?.Message;
                    stackTrace = exception.StackTrace;
                }
                _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = status;

        var body = new
        {
            success       = false,
            statusCode    = status,
            message,
            data          = payload,
            errors,
            detail,
            stackTrace,
            traceId,
            correlationId
        };

        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }
}
