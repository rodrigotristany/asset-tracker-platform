using System.Net;
using System.Text.Json;

namespace AssetTracker.Api.Middleware;

/// <summary>
/// Writes the codebase's standard error envelope ({"error", "message", "details"}) to an
/// HTTP response. Shared by <see cref="ErrorHandlingMiddleware"/> and the authentication
/// handlers/events so every 4xx/5xx response - whether it originates from an unhandled
/// exception or from the authentication pipeline itself - uses one JSON shape.
/// </summary>
public static class ErrorResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task WriteAsync(HttpContext context, HttpStatusCode statusCode, string error, string message, object? details = null)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var body = new { error, message, details };

        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
