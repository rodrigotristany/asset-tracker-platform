using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using AssetTracker.Api.Middleware;
using AssetTracker.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AssetTracker.Api.Auth;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private const string HeaderName = "X-API-Key";
    private readonly IDeviceRepository _deviceRepository;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IDeviceRepository deviceRepository)
        : base(options, logger, encoder)
    {
        _deviceRepository = deviceRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
            return AuthenticateResult.Fail("Missing X-API-Key header.");

        var apiKey = headerValues.ToString();

        byte[] apiKeyBytes;
        try
        {
            apiKeyBytes = Convert.FromBase64String(apiKey);
        }
        catch (FormatException)
        {
            return AuthenticateResult.Fail("Malformed API key.");
        }

        var apiKeyHash = SHA256.HashData(apiKeyBytes);
        var device = await _deviceRepository.GetByApiKeyHashAsync(apiKeyHash, Context.RequestAborted);

        if (device is null)
            return AuthenticateResult.Fail("Invalid API key.");

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, device.DeviceId) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return ErrorResponseWriter.WriteAsync(
            Context,
            HttpStatusCode.Unauthorized,
            "AUTHENTICATION_REQUIRED",
            "A valid X-API-Key header is required.");
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        return ErrorResponseWriter.WriteAsync(
            Context,
            HttpStatusCode.Forbidden,
            "FORBIDDEN",
            "You do not have permission to access this resource.");
    }
}
