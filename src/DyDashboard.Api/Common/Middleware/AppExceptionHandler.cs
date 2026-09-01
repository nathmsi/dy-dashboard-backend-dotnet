using System.Text.Json;
using DyDashboard.Api.Common.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace DyDashboard.Api.Common.Middleware;

/// <summary>
/// Central error handler: every unhandled exception ends up here and is rendered
/// as a consistent JSON envelope (<c>{ "error": { code, message, details? } }</c>).
/// Unknown errors are logged and surfaced as a generic 500 so internals never
/// leak to clients. This is the .NET equivalent of the Express error middleware.
/// </summary>
public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx, Exception exception, CancellationToken ct)
    {
        var (status, code, message, details) = Map(exception);

        if (status >= 500)
            logger.LogError(exception, "Unhandled error");

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";

        var payload = new
        {
            error = details is null
                ? (object)new { code, message }
                : new { code, message, details },
        };
        await ctx.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonOpts), ct);
        return true;
    }

    private static (int status, string code, string message, object? details) Map(Exception ex) => ex switch
    {
        AppException app => (app.StatusCode, app.Code, app.Message, app.Details),
        // Malformed JSON bodies surface as BadHttpRequestException from model binding.
        BadHttpRequestException => (400, "BAD_REQUEST", "Malformed JSON body", null),
        JsonException => (400, "BAD_REQUEST", "Malformed JSON body", null),
        _ => (500, "INTERNAL_ERROR", "Internal server error", null),
    };

    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);
}
