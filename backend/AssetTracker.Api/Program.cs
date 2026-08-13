using System.Net;
using System.Text;
using AssetTracker.Api.Auth;
using AssetTracker.Api.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var body = new
        {
            error = "VALIDATION_ERROR",
            message = "One or more validation errors occurred.",
            details = errors
        };

        return new BadRequestObjectResult(body);
    };
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddResponseCompression();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AuthSchemes.Jwt;
        options.DefaultChallengeScheme = AuthSchemes.Jwt;
    })
    .AddJwtBearer(AuthSchemes.Jwt, options =>
    {
        // Read Jwt:Key lazily (at options-binding time, not eagerly here in top-level Program.cs
        // code) so integration tests using WebApplicationFactory's ConfigureAppConfiguration
        // override (which is only merged into builder.Configuration as part of builder.Build())
        // see the overridden test key rather than the appsettings.json placeholder.
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key configuration is required.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Without these handlers, the JWT scheme writes a bare 401/403 with no body, breaking the
        // codebase-wide contract that every 4xx/5xx response uses the standard error envelope.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                return ErrorResponseWriter.WriteAsync(
                    context.HttpContext,
                    HttpStatusCode.Unauthorized,
                    "AUTHENTICATION_REQUIRED",
                    "A valid JWT bearer token is required.");
            },
            OnForbidden = context =>
                ErrorResponseWriter.WriteAsync(
                    context.HttpContext,
                    HttpStatusCode.Forbidden,
                    "FORBIDDEN",
                    "You do not have permission to access this resource.")
        };
    })
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthSchemes.ApiKey, _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

// AddInfrastructure reads ConnectionStrings:Default lazily (at DI-resolution time, per request)
// rather than eagerly at startup, to accommodate WebApplicationFactory's config-override timing
// in integration tests (see the comment in AddInfrastructure). That means a production deploy
// with a missing/blank connection string would otherwise start up successfully and only fail on
// the first real request. Resolve the fully-built (test-override-aware) configuration here, right
// after Build(), to restore fail-fast startup behavior without reintroducing the eager-read bug.
var startupConfiguration = app.Services.GetRequiredService<IConfiguration>();
if (string.IsNullOrWhiteSpace(startupConfiguration.GetConnectionString("Default")))
{
    throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseResponseCompression();

app.UseCors("DashboardDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
