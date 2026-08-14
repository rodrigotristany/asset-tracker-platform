using System.Net;
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

            // Domain exceptions that are explicitly mapped below carry an intentional, safe
            // client-facing message. Anything that falls through to the catch-all/500 branch is
            // unmapped and unexpected (e.g. a SQL exception, a misconfiguration) and may contain
            // internal details (connection strings, hostnames, stack info) - never echo
            // exception.Message to the client in that case, only log it server-side.
            var message = statusCode == HttpStatusCode.InternalServerError
                ? "An unexpected error occurred."
                : exception.Message;

            await ErrorResponseWriter.WriteAsync(context, statusCode, error, message);
        }
    }

    private static (HttpStatusCode StatusCode, string Error) MapException(Exception exception) => exception switch
    {
        DeviceNotFoundException => (HttpStatusCode.NotFound, "DEVICE_NOT_FOUND"),
        LocationNotFoundException => (HttpStatusCode.NotFound, "LOCATION_NOT_FOUND"),
        DeviceAlreadyExistsException => (HttpStatusCode.Conflict, "DEVICE_ALREADY_EXISTS"),
        InvalidCredentialsException => (HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS"),
        DeviceOwnershipMismatchException => (HttpStatusCode.Forbidden, "FORBIDDEN"),
        _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
    };
}
