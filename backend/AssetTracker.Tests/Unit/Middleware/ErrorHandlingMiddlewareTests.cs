using System.Net;
using System.Text.Json;
using AssetTracker.Api.Middleware;
using AssetTracker.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AssetTracker.Tests.Unit.Middleware;

/// <summary>
/// Exercises ErrorHandlingMiddleware directly against a real HttpContext/HttpResponse, asserting
/// on the actual status code and JSON body written to the response - i.e. the same observable
/// contract a real HTTP client would see.
///
/// DeviceNotFoundException is exercised here (rather than via a full WebApplicationFactory HTTP
/// call) because after the Critical #1 fix (LocationsController.EnsureDeviceOwnership), a caller
/// authenticated via a valid API key can no longer reach LocationService with a deviceId other
/// than their own - any mismatch (whether the target device exists or not) is now rejected as
/// 403 Forbidden before the service layer's device lookup ever runs. That is the intended,
/// more-secure outcome of that fix. DeviceNotFoundException is therefore no longer reachable
/// through a real production route, so its middleware mapping is verified at this level instead.
/// </summary>
public class ErrorHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, JsonElement Body)> InvokeAsync(Exception exceptionToThrow)
    {
        var middleware = new ErrorHandlingMiddleware(_ => throw exceptionToThrow, NullLogger<ErrorHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, JsonDocument.Parse(json).RootElement.Clone());
    }

    [Fact]
    public async Task InvokeAsync_WithDeviceNotFoundException_Returns404WithDeviceNotFoundError()
    {
        var (statusCode, body) = await InvokeAsync(new DeviceNotFoundException("missing-device"));

        Assert.Equal((int)HttpStatusCode.NotFound, statusCode);
        Assert.Equal("DEVICE_NOT_FOUND", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithLocationNotFoundException_Returns404WithLocationNotFoundError()
    {
        var (statusCode, body) = await InvokeAsync(new LocationNotFoundException("missing-device"));

        Assert.Equal((int)HttpStatusCode.NotFound, statusCode);
        Assert.Equal("LOCATION_NOT_FOUND", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithDeviceOwnershipMismatchException_Returns403WithForbiddenError()
    {
        var (statusCode, body) = await InvokeAsync(new DeviceOwnershipMismatchException("device-b", "device-a"));

        Assert.Equal((int)HttpStatusCode.Forbidden, statusCode);
        Assert.Equal("FORBIDDEN", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithUnmappedException_Returns500WithGenericMessageOnly()
    {
        var (statusCode, body) = await InvokeAsync(
            new InvalidOperationException("Connection failed: Server=prod-db-01;Password=super-secret-value;"));

        Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);
        Assert.Equal("INTERNAL_ERROR", body.GetProperty("error").GetString());
        Assert.Equal("An unexpected error occurred.", body.GetProperty("message").GetString());
    }
}
