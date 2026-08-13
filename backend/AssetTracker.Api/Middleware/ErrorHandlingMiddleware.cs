using System.Net;
using System.Text.Json;
using AssetTracker.Application.Exceptions;

namespace AssetTracker.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var (statusCode, error) = MapException(exception);

            _logger.LogError(exception, "Unhandled exception mapped to {StatusCode} {Error} for request {RequestId}",
                statusCode, error, context.TraceIdentifier);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var body = new
            {
                error,
                message = exception.Message,
                details = (object?)null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
    }

    private static (HttpStatusCode StatusCode, string Error) MapException(Exception exception) => exception switch
    {
        DeviceNotFoundException => (HttpStatusCode.NotFound, "DEVICE_NOT_FOUND"),
        DeviceAlreadyExistsException => (HttpStatusCode.Conflict, "DEVICE_ALREADY_EXISTS"),
        InvalidCredentialsException => (HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS"),
        _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
    };
}
