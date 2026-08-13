# Backend C#/.NET Rewrite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the unimplemented Python/FastAPI backend spec with a working ASP.NET Core / SQL Server backend (Controller MVC, hybrid EF Core + stored procedures, Clean Architecture, JWT + API-key auth), then rewrite the project's specs/rules docs to match.

**Architecture:** Four-project Clean Architecture (`Domain` → `Application` → `Infrastructure` → `Api`), plus a test project. Reads and simple CRUD go through EF Core; the write-heavy/interview-relevant paths (location insert/batch, device registration/lookup, retention purge) go through hand-written SQL Server stored procedures called via Dapper/ADO.NET.

**Tech Stack:** .NET 10 (confirmed installed via `dotnet-sdk-10.0`), ASP.NET Core Controller MVC, EF Core 10 (SQL Server provider), Dapper, SQL Server (`mcr.microsoft.com/mssql/server:2022-latest`), xUnit + Testcontainers.MsSql, Moq, Swashbuckle, Azure Pipelines.

## Global Constraints

- Target framework: `net10.0` (verified installed: SDK `10.0.110`).
- Database: SQL Server only — no PostgreSQL, no SQLite/in-memory fakes for integration tests (carried over from `.clinerules/testing.md`'s "no in-memory fakes for DB parity" rule).
- API style: Controller-based MVC (`[ApiController]`), not Minimal APIs.
- Data access: EF Core for reads/simple CRUD (`AdminUser`); stored procedures via Dapper for `Location` and `Device` write/lookup paths and retention purge (per approved design).
- JSON payloads: camelCase (ASP.NET Core's default `System.Text.Json` policy — no custom attributes needed).
- Standardized error envelope: `{"error": "...", "message": "...", "details": {...}}` for all 4xx/5xx responses.
- C# naming: PascalCase for types/members, camelCase for locals/parameters (this supersedes the blanket "snake_case functions/variables" rule in `.clinerules/coding.md`, which is Python/C++-flavored — Task 14 fixes this in the doc itself).
- Database naming: tables plural `snake_case`, columns `snake_case` (carried over from `.clinerules/backend.md`, still correct since it governs SQL, not C#).
- No hardcoded secrets: `appsettings.json` holds obviously-fake local-dev defaults (e.g. `Jwt:Key`, DB password); real values come from environment variables via ASP.NET Core's config system (`ConnectionStrings__Default`, `Jwt__Key`, etc.) — this is the .NET-native equivalent of the original spec's `pydantic-settings`/`.env` rule.
- Every controller endpoint needs an integration test covering happy path, validation errors, and auth failures (carried over from `.clinerules/testing.md`).
- Exact NuGet package versions (verified available via `dotnet package search` against nuget.org on 2026-08-12 — pin these, don't let tooling auto-resolve newer):

| Package | Version | Used by |
|---|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.11 | Infrastructure |
| Microsoft.EntityFrameworkCore.Design | 10.0.11 | Infrastructure (dev-time only) |
| Microsoft.Data.SqlClient | 7.0.2 | Infrastructure, Tests |
| Dapper | 2.1.79 | Infrastructure |
| Testcontainers.MsSql | 4.13.0 | Tests |
| BCrypt.Net-Next | 4.2.0 | Infrastructure, Tests |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.11 | Api |
| System.IdentityModel.Tokens.Jwt | 8.22.0 | Infrastructure |
| Microsoft.IdentityModel.Tokens | 8.22.0 | Infrastructure |
| Microsoft.Extensions.Options | 10.0.11 | Infrastructure |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.11 | Infrastructure |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.11 | Application |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.11 | Application, Infrastructure |
| Moq | 4.20.72 | Tests |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.11 | Tests |
| Swashbuckle.AspNetCore | 10.2.3 | Api |
| dotnet-ef (global tool) | 10.0.11 | dev machine |

- `dotnet new xunit`/`webapi`/`classlib` templates on this SDK default to: xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4 — leave these at template defaults.

## File Structure

```
backend/
├── AssetTracker.sln
├── global.json                          # pins SDK to 10.0.110
├── docker-compose.yml
├── azure-pipelines.yml                  # actually at repo root — see Task 13
├── AssetTracker.Domain/
│   ├── AssetTracker.Domain.csproj
│   └── Entities/
│       ├── Device.cs
│       ├── Location.cs
│       └── AdminUser.cs
├── AssetTracker.Application/
│   ├── AssetTracker.Application.csproj
│   ├── DependencyInjection.cs
│   ├── Dtos/
│   ├── Interfaces/
│   ├── Exceptions/
│   └── Services/
├── AssetTracker.Infrastructure/
│   ├── AssetTracker.Infrastructure.csproj
│   ├── DependencyInjection.cs
│   ├── Data/
│   │   ├── AssetTrackerDbContext.cs
│   │   ├── Migrations/
│   │   └── StoredProcedures/            # .sql scripts, applied via migrationBuilder.Sql()
│   ├── Repositories/
│   └── Security/
├── AssetTracker.Api/
│   ├── AssetTracker.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Dockerfile
│   ├── Controllers/
│   ├── Auth/
│   └── Middleware/
└── AssetTracker.Tests/
    ├── AssetTracker.Tests.csproj
    ├── Unit/
    │   ├── Domain/
    │   ├── Services/
    │   └── Security/
    └── Integration/
```

Each `DependencyInjection.cs` is a `Microsoft.Extensions.DependencyInjection`-namespaced extension-method file (`AddInfrastructure`, `AddApplicationServices`) that grows across tasks — this keeps `Program.cs` stable instead of touching it in nearly every task.

---

## Task 1: Solution Scaffolding + Health Endpoint

**Files:**
- Create: `backend/global.json`
- Create: `backend/AssetTracker.sln`
- Create: `backend/AssetTracker.Domain/AssetTracker.Domain.csproj`
- Create: `backend/AssetTracker.Application/AssetTracker.Application.csproj`
- Create: `backend/AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj`
- Create: `backend/AssetTracker.Api/AssetTracker.Api.csproj`, `Program.cs`, `appsettings.json`, `Controllers/HealthController.cs`
- Create: `backend/AssetTracker.Tests/AssetTracker.Tests.csproj`, `Integration/HealthEndpointTests.cs`
- Modify: `.gitignore` (append .NET section)

**Interfaces:**
- Produces: `Program` (public partial class, required for `WebApplicationFactory<Program>` in every later integration test task)

- [ ] **Step 1: Scaffold the solution and projects**

```bash
mkdir -p backend && cd backend
dotnet new globaljson --sdk-version 10.0.110
dotnet new sln -n AssetTracker

dotnet new classlib -n AssetTracker.Domain -o AssetTracker.Domain
dotnet new classlib -n AssetTracker.Application -o AssetTracker.Application
dotnet new classlib -n AssetTracker.Infrastructure -o AssetTracker.Infrastructure
dotnet new webapi -o AssetTracker.Api --use-controllers
dotnet new xunit -n AssetTracker.Tests -o AssetTracker.Tests

# webapi template doesn't take -n; rename its csproj/assembly to match the folder convention
mv AssetTracker.Api/AssetTracker.Api.csproj AssetTracker.Api/AssetTracker.Api.csproj 2>/dev/null || true
```

Note: `dotnet new webapi -o AssetTracker.Api --use-controllers` names the project after the containing folder automatically (`AssetTracker.Api.csproj`), so no rename is actually needed — the no-op line above is a safety check; if the generated file is instead named after the current directory, rename it to `AssetTracker.Api.csproj` and update `AssemblyName`/`RootNamespace` in the csproj to `AssetTracker.Api`.

Remove template cruft:
```bash
rm -f AssetTracker.Api/WeatherForecast.cs AssetTracker.Api/Controllers/WeatherForecastController.cs
```

- [ ] **Step 2: Wire solution references**

```bash
dotnet sln AssetTracker.sln add AssetTracker.Domain/AssetTracker.Domain.csproj
dotnet sln AssetTracker.sln add AssetTracker.Application/AssetTracker.Application.csproj
dotnet sln AssetTracker.sln add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj
dotnet sln AssetTracker.sln add AssetTracker.Api/AssetTracker.Api.csproj
dotnet sln AssetTracker.sln add AssetTracker.Tests/AssetTracker.Tests.csproj

dotnet add AssetTracker.Application/AssetTracker.Application.csproj reference AssetTracker.Domain/AssetTracker.Domain.csproj
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj reference AssetTracker.Application/AssetTracker.Application.csproj
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj reference AssetTracker.Domain/AssetTracker.Domain.csproj
dotnet add AssetTracker.Api/AssetTracker.Api.csproj reference AssetTracker.Application/AssetTracker.Application.csproj
dotnet add AssetTracker.Api/AssetTracker.Api.csproj reference AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj reference AssetTracker.Domain/AssetTracker.Domain.csproj
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj reference AssetTracker.Application/AssetTracker.Application.csproj
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj reference AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj reference AssetTracker.Api/AssetTracker.Api.csproj
```

- [ ] **Step 3: Replace `AssetTracker.Api/Program.cs`**

Remove the `Microsoft.AspNetCore.OpenApi` package (we use Swashbuckle later instead):
```bash
dotnet remove AssetTracker.Api/AssetTracker.Api.csproj package Microsoft.AspNetCore.OpenApi
```

Write `backend/AssetTracker.Api/Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 4: Add `Microsoft.AspNetCore.Mvc.Testing` to the test project and write the failing test**

```bash
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.11
```

Write `backend/AssetTracker.Tests/Integration/HealthEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AssetTracker.Tests.Integration;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOkWithStatus()
    {
        var response = await _client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", body!["status"]);
    }
}
```

- [ ] **Step 5: Run the test, verify it fails**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter GetHealth_ReturnsOkWithStatus`
Expected: FAIL — `Assert.Equal() Failure: Expected: OK, Actual: NotFound` (no route registered yet).

- [ ] **Step 6: Implement `HealthController`**

Write `backend/AssetTracker.Api/Controllers/HealthController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok" });
}
```

- [ ] **Step 7: Run the test, verify it passes**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter GetHealth_ReturnsOkWithStatus`
Expected: PASS

- [ ] **Step 8: `.gitignore`, docker files placeholder, commit**

Append to root `.gitignore` (it currently only covers C++/CMake — see existing content, don't remove any of it):
```
# .NET
backend/**/bin/
backend/**/obj/
backend/**/*.user
```

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/ .gitignore
git commit -m "$(cat <<'EOF'
RT: backend scaffolding — Clean Architecture solution + health endpoint

Sets up the 4-project Clean Architecture skeleton (Domain/Application/
Infrastructure/Api) plus the test project, with a working health check
as the first vertical slice.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 2: Domain Entities

**Files:**
- Create: `backend/AssetTracker.Domain/Entities/Device.cs`
- Create: `backend/AssetTracker.Domain/Entities/Location.cs`
- Create: `backend/AssetTracker.Domain/Entities/AdminUser.cs`
- Create: `backend/AssetTracker.Tests/Unit/Domain/DeviceTests.cs`
- Create: `backend/AssetTracker.Tests/Unit/Domain/LocationTests.cs`
- Create: `backend/AssetTracker.Tests/Unit/Domain/AdminUserTests.cs`

**Interfaces:**
- Produces:
  - `Device(string deviceId, byte[] apiKeyHash, string? displayName = null)`, `Device.Reconstitute(int id, string deviceId, byte[] apiKeyHash, string? displayName, bool isActive, DateTime createdAt)`, properties `Id, DeviceId, DisplayName, ApiKeyHash, IsActive, CreatedAt`
  - `Location(int deviceFk, DateTimeOffset timestamp, double latitude, double longitude, double? altitude, double? speed, byte? satellites, double? hdop, double? batteryVoltage, bool isStale)`, `Location.Reconstitute(long id, int deviceFk, DateTimeOffset timestamp, double latitude, double longitude, double? altitude, double? speed, byte? satellites, double? hdop, double? batteryVoltage, bool isStale, DateTime createdAt)`, properties `Id, DeviceFk, Timestamp, Latitude, Longitude, Altitude, Speed, Satellites, Hdop, BatteryVoltage, IsStale, CreatedAt`
  - `AdminUser(string username, string passwordHash)`, properties `Id, Username, PasswordHash, CreatedAt`

Domain entities validate invariants in their public constructor but expose a `Reconstitute` static factory (Device/Location only) for repositories to rehydrate already-validated data from Dapper query results without re-running validation. `AdminUser` doesn't need one — it's read via EF Core, which populates private setters through its own materialization path regardless of constructor access.

- [ ] **Step 1: Write the failing tests**

Write `backend/AssetTracker.Tests/Unit/Domain/DeviceTests.cs`:
```csharp
using AssetTracker.Domain.Entities;
using Xunit;

namespace AssetTracker.Tests.Unit.Domain;

public class DeviceTests
{
    [Fact]
    public void Constructor_WithEmptyDeviceId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Device("", new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public void Constructor_WithEmptyApiKeyHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Device("goat-001", Array.Empty<byte>()));
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsDefaults()
    {
        var device = new Device("goat-001", new byte[] { 1, 2, 3 }, "Goat 001");

        Assert.Equal("goat-001", device.DeviceId);
        Assert.Equal("Goat 001", device.DisplayName);
        Assert.True(device.IsActive);
    }

    [Fact]
    public void Reconstitute_PreservesAllFields()
    {
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var hash = new byte[] { 9, 9, 9 };

        var device = Device.Reconstitute(42, "goat-002", hash, "Goat 002", false, createdAt);

        Assert.Equal(42, device.Id);
        Assert.Equal("goat-002", device.DeviceId);
        Assert.Equal(hash, device.ApiKeyHash);
        Assert.Equal("Goat 002", device.DisplayName);
        Assert.False(device.IsActive);
        Assert.Equal(createdAt, device.CreatedAt);
    }
}
```

Write `backend/AssetTracker.Tests/Unit/Domain/LocationTests.cs`:
```csharp
using AssetTracker.Domain.Entities;
using Xunit;

namespace AssetTracker.Tests.Unit.Domain;

public class LocationTests
{
    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Constructor_WithInvalidLatitude_Throws(double latitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Location(1, DateTimeOffset.UtcNow, latitude, 0, null, null, null, null, null, false));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Constructor_WithInvalidLongitude_Throws(double longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Location(1, DateTimeOffset.UtcNow, 0, longitude, null, null, null, null, null, false));
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsFields()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var location = new Location(7, timestamp, -31.4231, -62.0834, 142.1, 0.4, 9, 0.8, 3.7, false);

        Assert.Equal(7, location.DeviceFk);
        Assert.Equal(timestamp, location.Timestamp);
        Assert.Equal(-31.4231, location.Latitude);
        Assert.False(location.IsStale);
    }

    [Fact]
    public void Reconstitute_PreservesAllFields()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var createdAt = DateTime.UtcNow;

        var location = Location.Reconstitute(100, 7, timestamp, 1.1, 2.2, 3.3, 4.4, 5, 6.6, 7.7, true, createdAt);

        Assert.Equal(100, location.Id);
        Assert.Equal(7, location.DeviceFk);
        Assert.True(location.IsStale);
        Assert.Equal(createdAt, location.CreatedAt);
    }
}
```

Write `backend/AssetTracker.Tests/Unit/Domain/AdminUserTests.cs`:
```csharp
using AssetTracker.Domain.Entities;
using Xunit;

namespace AssetTracker.Tests.Unit.Domain;

public class AdminUserTests
{
    [Fact]
    public void Constructor_WithEmptyUsername_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AdminUser("", "hash"));
    }

    [Fact]
    public void Constructor_WithEmptyPasswordHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => new AdminUser("admin", ""));
    }

    [Fact]
    public void Constructor_WithValidArgs_SetsFields()
    {
        var user = new AdminUser("admin", "$2a$11$hashedvalue");

        Assert.Equal("admin", user.Username);
        Assert.Equal("$2a$11$hashedvalue", user.PasswordHash);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~Unit.Domain"`
Expected: FAIL to build — `error CS0234: The type or namespace name 'Entities' does not exist in the namespace 'AssetTracker.Domain'`

- [ ] **Step 3: Implement the entities**

Write `backend/AssetTracker.Domain/Entities/Device.cs`:
```csharp
namespace AssetTracker.Domain.Entities;

public class Device
{
    public int Id { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public byte[] ApiKeyHash { get; private set; } = Array.Empty<byte>();
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Device() { }

    public Device(string deviceId, byte[] apiKeyHash, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("Device ID is required.", nameof(deviceId));
        if (apiKeyHash is null || apiKeyHash.Length == 0)
            throw new ArgumentException("API key hash is required.", nameof(apiKeyHash));

        DeviceId = deviceId;
        ApiKeyHash = apiKeyHash;
        DisplayName = displayName;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Device Reconstitute(int id, string deviceId, byte[] apiKeyHash, string? displayName, bool isActive, DateTime createdAt)
    {
        return new Device
        {
            Id = id,
            DeviceId = deviceId,
            ApiKeyHash = apiKeyHash,
            DisplayName = displayName,
            IsActive = isActive,
            CreatedAt = createdAt
        };
    }
}
```

Write `backend/AssetTracker.Domain/Entities/Location.cs`:
```csharp
namespace AssetTracker.Domain.Entities;

public class Location
{
    public long Id { get; private set; }
    public int DeviceFk { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double? Altitude { get; private set; }
    public double? Speed { get; private set; }
    public byte? Satellites { get; private set; }
    public double? Hdop { get; private set; }
    public double? BatteryVoltage { get; private set; }
    public bool IsStale { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Location() { }

    public Location(
        int deviceFk,
        DateTimeOffset timestamp,
        double latitude,
        double longitude,
        double? altitude,
        double? speed,
        byte? satellites,
        double? hdop,
        double? batteryVoltage,
        bool isStale)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        DeviceFk = deviceFk;
        Timestamp = timestamp;
        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
        Speed = speed;
        Satellites = satellites;
        Hdop = hdop;
        BatteryVoltage = batteryVoltage;
        IsStale = isStale;
        CreatedAt = DateTime.UtcNow;
    }

    public static Location Reconstitute(
        long id,
        int deviceFk,
        DateTimeOffset timestamp,
        double latitude,
        double longitude,
        double? altitude,
        double? speed,
        byte? satellites,
        double? hdop,
        double? batteryVoltage,
        bool isStale,
        DateTime createdAt)
    {
        return new Location
        {
            Id = id,
            DeviceFk = deviceFk,
            Timestamp = timestamp,
            Latitude = latitude,
            Longitude = longitude,
            Altitude = altitude,
            Speed = speed,
            Satellites = satellites,
            Hdop = hdop,
            BatteryVoltage = batteryVoltage,
            IsStale = isStale,
            CreatedAt = createdAt
        };
    }
}
```

Write `backend/AssetTracker.Domain/Entities/AdminUser.cs`:
```csharp
namespace AssetTracker.Domain.Entities;

public class AdminUser
{
    public int Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private AdminUser() { }

    public AdminUser(string username, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        Username = username;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~Unit.Domain"`
Expected: PASS (10 tests)

- [ ] **Step 5: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Domain backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: domain entities — Device, Location, AdminUser

Rich domain model with validating constructors plus a Reconstitute
factory for rehydrating entities from stored-procedure query results
without re-running write-path validation.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 3: EF Core DbContext + Initial Migration + Testcontainers Fixture

**Files:**
- Create: `backend/AssetTracker.Infrastructure/Data/AssetTrackerDbContext.cs`
- Create: `backend/AssetTracker.Infrastructure/DependencyInjection.cs`
- Create: `backend/AssetTracker.Api/appsettings.json` (already exists from template — overwrite)
- Create: `backend/AssetTracker.Tests/Integration/SqlServerFixture.cs`
- Create: `backend/AssetTracker.Tests/Integration/DatabaseSchemaTests.cs`
- Modify: `backend/AssetTracker.Api/Program.cs`

**Interfaces:**
- Consumes: none new
- Produces: `AssetTrackerDbContext` (DbSets `Devices`, `Locations`, `AdminUsers`), `AddInfrastructure(this IServiceCollection, IConfiguration)`, `SqlServerFixture` (test-only: `ConnectionString`, `CreateDbContext()`), `[Collection("Database")]` xUnit collection

- [ ] **Step 1: Add packages**

```bash
cd backend
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.11
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.11
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Microsoft.Extensions.Configuration.Abstractions --version 10.0.11
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj package Testcontainers.MsSql --version 4.13.0
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj package Microsoft.Data.SqlClient --version 7.0.2
dotnet tool install --global dotnet-ef --version 10.0.11
```

- [ ] **Step 2: Write `AssetTrackerDbContext`**

Write `backend/AssetTracker.Infrastructure/Data/AssetTrackerDbContext.cs`:
```csharp
using AssetTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.Infrastructure.Data;

public class AssetTrackerDbContext : DbContext
{
    public AssetTrackerDbContext(DbContextOptions<AssetTrackerDbContext> options) : base(options) { }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Id).HasColumnName("id");
            entity.Property(d => d.DeviceId).HasColumnName("device_id").HasMaxLength(64).IsRequired();
            entity.HasIndex(d => d.DeviceId).IsUnique();
            entity.Property(d => d.DisplayName).HasColumnName("display_name").HasMaxLength(128);
            entity.Property(d => d.ApiKeyHash).HasColumnName("api_key_hash").HasMaxLength(64).IsRequired();
            entity.Property(d => d.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(d => d.CreatedAt).HasColumnName("created_at").IsRequired();
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("locations");
            entity.HasKey(l => l.Id);
            entity.Property(l => l.Id).HasColumnName("id");
            entity.Property(l => l.DeviceFk).HasColumnName("device_fk").IsRequired();
            entity.Property(l => l.Timestamp).HasColumnName("timestamp").IsRequired();
            entity.Property(l => l.Latitude).HasColumnName("latitude").IsRequired();
            entity.Property(l => l.Longitude).HasColumnName("longitude").IsRequired();
            entity.Property(l => l.Altitude).HasColumnName("altitude");
            entity.Property(l => l.Speed).HasColumnName("speed");
            entity.Property(l => l.Satellites).HasColumnName("satellites");
            entity.Property(l => l.Hdop).HasColumnName("hdop");
            entity.Property(l => l.BatteryVoltage).HasColumnName("battery_voltage");
            entity.Property(l => l.IsStale).HasColumnName("is_stale").IsRequired();
            entity.Property(l => l.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.HasOne<Device>().WithMany().HasForeignKey(l => l.DeviceFk).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(l => new { l.DeviceFk, l.Timestamp }).HasDatabaseName("idx_locations_device_timestamp");
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Id).HasColumnName("id");
            entity.Property(a => a.Username).HasColumnName("username").HasMaxLength(64).IsRequired();
            entity.HasIndex(a => a.Username).IsUnique();
            entity.Property(a => a.PasswordHash).HasColumnName("password_hash").HasColumnType("varchar(60)").IsRequired();
            entity.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
        });
    }
}
```

Note on `password_hash varchar(60)`: this supersedes the design doc's `VARBINARY(64)` for that one column. BCrypt produces a self-contained encoded string (algorithm + cost + salt + hash, always 60 ASCII chars), not raw bytes you compare — it doesn't fit a binary column. `devices.api_key_hash` stays `VARBINARY(64)` as designed, since that column holds a raw SHA-256 digest (32 bytes), which does fit.

- [ ] **Step 3: Write `AddInfrastructure` DI extension**

Write `backend/AssetTracker.Infrastructure/DependencyInjection.cs`:
```csharp
using AssetTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");

        services.AddDbContext<AssetTrackerDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
```

This file's namespace is deliberately `Microsoft.Extensions.DependencyInjection` (not `AssetTracker.Infrastructure`) — ASP.NET Core Web SDK projects implicitly `using` that namespace, so `Program.cs` can call `builder.Services.AddInfrastructure(...)` with no extra `using` statement. `IServiceCollection` itself needs no `using` for the same reason (it's declared in this same namespace) — but `IConfiguration` lives in `Microsoft.Extensions.Configuration`, a plain class library gets no implicit usings for it, so it's spelled out explicitly. This file grows in Tasks 4, 5, 6, 7, and 9 as repositories and security services are added.

- [ ] **Step 4: Set `appsettings.json` and wire `Program.cs`**

Overwrite `backend/AssetTracker.Api/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=AssetTrackerDb;User Id=sa;Password=changeme;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_BYTES",
    "Issuer": "AssetTrackerApi",
    "Audience": "AssetTrackerDashboard",
    "ExpiryMinutes": 60
  }
}
```

These are obviously-fake local-dev defaults, not real secrets. Production overrides them via environment variables (`ConnectionStrings__Default`, `Jwt__Key`) — never edit this file with a real password.

Replace `backend/AssetTracker.Api/Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 5: Write the Testcontainers fixture and the failing schema test**

Write `backend/AssetTracker.Tests/Integration/SqlServerFixture.cs`:
```csharp
using AssetTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace AssetTracker.Tests.Integration;

public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public AssetTrackerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetTrackerDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AssetTrackerDbContext(options);
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<SqlServerFixture>
{
}
```

Write `backend/AssetTracker.Tests/Integration/DatabaseSchemaTests.cs`:
```csharp
using Microsoft.Data.SqlClient;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class DatabaseSchemaTests
{
    private readonly SqlServerFixture _fixture;

    public DatabaseSchemaTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("devices")]
    [InlineData("locations")]
    [InlineData("admin_users")]
    public async Task Migration_CreatesExpectedTable(string tableName)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName";
        command.Parameters.AddWithValue("@TableName", tableName);

        var count = (int)(await command.ExecuteScalarAsync())!;

        Assert.Equal(1, count);
    }
}
```

- [ ] **Step 6: Run tests, verify they fail**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~DatabaseSchemaTests"`
Expected: FAIL — container starts fine, `MigrateAsync()` is a no-op (no migrations exist yet), so `INFORMATION_SCHEMA.TABLES` has no matching rows: `Assert.Equal() Failure: Expected: 1, Actual: 0` for all three tables. (Requires Docker running — confirmed available on this machine.)

- [ ] **Step 7: Generate the initial migration**

```bash
cd backend
dotnet ef migrations add InitialCreate \
  --project AssetTracker.Infrastructure \
  --startup-project AssetTracker.Api \
  --output-dir Data/Migrations
```

- [ ] **Step 8: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~DatabaseSchemaTests"`
Expected: PASS (3 tests)

- [ ] **Step 9: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Infrastructure backend/AssetTracker.Api backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: EF Core DbContext + initial migration + Testcontainers fixture

Adds the SQL Server schema (devices/locations/admin_users) via EF Core
code-first migration, and a shared Testcontainers SQL Server fixture
used by every integration test from here on.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 4: Device Stored Procedures + DeviceRepository

**Files:**
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Device_Register.sql`
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Device_GetByApiKeyHash.sql`
- Create: `backend/AssetTracker.Application/Interfaces/IDeviceRepository.cs`
- Create: `backend/AssetTracker.Infrastructure/Repositories/DeviceRepository.cs`
- Create: `backend/AssetTracker.Tests/Integration/DeviceRepositoryTests.cs`
- Modify: `backend/AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj` (add Dapper, Microsoft.Data.SqlClient)
- Modify: `backend/AssetTracker.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `AssetTrackerDbContext` (Task 3)
- Produces: `IDeviceRepository { Task<Device> RegisterAsync(string deviceId, byte[] apiKeyHash, string? displayName, CancellationToken ct); Task<Device?> GetByApiKeyHashAsync(byte[] apiKeyHash, CancellationToken ct); Task<Device?> GetByDeviceIdAsync(string deviceId, CancellationToken ct); }`

`RegisterAsync`/`GetByApiKeyHashAsync` go through stored procedures (Dapper). `GetByDeviceIdAsync` is a simple EF Core LINQ read — per the approved hybrid design, only the two stored-procedure-worthy operations get procs; a plain lookup by unique key doesn't need one.

- [ ] **Step 1: Add packages**

```bash
cd backend
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Dapper --version 2.1.79
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Microsoft.Data.SqlClient --version 7.0.2
```

- [ ] **Step 2: Write the stored procedure scripts**

Write `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Device_Register.sql`:
```sql
CREATE OR ALTER PROCEDURE usp_Device_Register
    @DeviceId NVARCHAR(64),
    @DisplayName NVARCHAR(128) = NULL,
    @ApiKeyHash VARBINARY(64)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM devices WHERE device_id = @DeviceId)
    BEGIN
        THROW 50001, 'Device already exists.', 1;
    END

    INSERT INTO devices (device_id, display_name, api_key_hash, is_active, created_at)
    OUTPUT
        INSERTED.id AS Id,
        INSERTED.device_id AS DeviceId,
        INSERTED.display_name AS DisplayName,
        INSERTED.api_key_hash AS ApiKeyHash,
        INSERTED.is_active AS IsActive,
        INSERTED.created_at AS CreatedAt
    VALUES (@DeviceId, @DisplayName, @ApiKeyHash, 1, SYSUTCDATETIME());
END
```

Write `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Device_GetByApiKeyHash.sql`:
```sql
CREATE OR ALTER PROCEDURE usp_Device_GetByApiKeyHash
    @ApiKeyHash VARBINARY(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        id AS Id,
        device_id AS DeviceId,
        display_name AS DisplayName,
        api_key_hash AS ApiKeyHash,
        is_active AS IsActive,
        created_at AS CreatedAt
    FROM devices
    WHERE api_key_hash = @ApiKeyHash AND is_active = 1;
END
```

Output/select columns are aliased to PascalCase to match Dapper's default (case-insensitive, no underscore-splitting) column-to-property mapping — this keeps the C# side free of custom type maps.

`CREATE OR ALTER` requires SQL Server 2016+; our dev/CI target is the `2022-latest` container image, so this is safe. (A literal SQL Server 2008 target, per the lower bound the JD mentions, would need the older `IF OBJECT_ID(...) IS NULL CREATE PROCEDURE ... ELSE ALTER PROCEDURE ...` pattern — worth mentioning if it comes up in the interview, not needed here.)

- [ ] **Step 3: Write `IDeviceRepository` and the failing integration test**

Write `backend/AssetTracker.Application/Interfaces/IDeviceRepository.cs`:
```csharp
using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface IDeviceRepository
{
    Task<Device> RegisterAsync(string deviceId, byte[] apiKeyHash, string? displayName, CancellationToken ct);
    Task<Device?> GetByApiKeyHashAsync(byte[] apiKeyHash, CancellationToken ct);
    Task<Device?> GetByDeviceIdAsync(string deviceId, CancellationToken ct);
}
```

Write `backend/AssetTracker.Tests/Integration/DeviceRepositoryTests.cs`:
```csharp
using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class DeviceRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public DeviceRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private DeviceRepository CreateRepository() =>
        new(_fixture.ConnectionString, _fixture.CreateDbContext());

    [Fact]
    public async Task RegisterAsync_ThenGetByApiKeyHash_ReturnsSameDevice()
    {
        var repository = CreateRepository();
        var deviceId = $"test-device-{Guid.NewGuid():N}";
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);

        var registered = await repository.RegisterAsync(deviceId, apiKeyHash, "Test Device", CancellationToken.None);
        var fetched = await repository.GetByApiKeyHashAsync(apiKeyHash, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(registered.Id, fetched!.Id);
        Assert.Equal(deviceId, fetched.DeviceId);
        Assert.Equal("Test Device", fetched.DisplayName);
        Assert.True(fetched.IsActive);
    }

    [Fact]
    public async Task GetByApiKeyHash_WithUnknownHash_ReturnsNull()
    {
        var repository = CreateRepository();
        var unknownHash = new byte[32];
        Random.Shared.NextBytes(unknownHash);

        var result = await repository.GetByApiKeyHashAsync(unknownHash, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByDeviceIdAsync_AfterRegister_ReturnsDevice()
    {
        var repository = CreateRepository();
        var deviceId = $"test-device-{Guid.NewGuid():N}";
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);
        await repository.RegisterAsync(deviceId, apiKeyHash, null, CancellationToken.None);

        var result = await repository.GetByDeviceIdAsync(deviceId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(deviceId, result!.DeviceId);
    }

    [Fact]
    public async Task GetByDeviceIdAsync_WithUnknownDeviceId_ReturnsNull()
    {
        var repository = CreateRepository();

        var result = await repository.GetByDeviceIdAsync("does-not-exist", CancellationToken.None);

        Assert.Null(result);
    }
}
```

- [ ] **Step 4: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~DeviceRepositoryTests"`
Expected: FAIL to build — `DeviceRepository` doesn't exist yet.

- [ ] **Step 5: Implement `DeviceRepository`**

Write `backend/AssetTracker.Infrastructure/Repositories/DeviceRepository.cs`:
```csharp
using System.Data;
using AssetTracker.Application.Interfaces;
using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.Infrastructure.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly string _connectionString;
    private readonly AssetTrackerDbContext _dbContext;

    public DeviceRepository(string connectionString, AssetTrackerDbContext dbContext)
    {
        _connectionString = connectionString;
        _dbContext = dbContext;
    }

    public async Task<Device> RegisterAsync(string deviceId, byte[] apiKeyHash, string? displayName, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleAsync<DeviceRow>(
            new CommandDefinition(
                "usp_Device_Register",
                new { DeviceId = deviceId, DisplayName = displayName, ApiKeyHash = apiKeyHash },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return row.ToEntity();
    }

    public async Task<Device?> GetByApiKeyHashAsync(byte[] apiKeyHash, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<DeviceRow>(
            new CommandDefinition(
                "usp_Device_GetByApiKeyHash",
                new { ApiKeyHash = apiKeyHash },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return row?.ToEntity();
    }

    public async Task<Device?> GetByDeviceIdAsync(string deviceId, CancellationToken ct)
    {
        var row = await _dbContext.Devices
            .AsNoTracking()
            .Where(d => d.DeviceId == deviceId)
            .Select(d => new DeviceRow
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                DisplayName = d.DisplayName,
                ApiKeyHash = d.ApiKeyHash,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .SingleOrDefaultAsync(ct);

        return row?.ToEntity();
    }

    private sealed class DeviceRow
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public byte[] ApiKeyHash { get; set; } = Array.Empty<byte>();
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public Device ToEntity() => Device.Reconstitute(Id, DeviceId, ApiKeyHash, DisplayName, IsActive, CreatedAt);
    }
}
```

- [ ] **Step 6: Register in DI**

Modify `backend/AssetTracker.Infrastructure/DependencyInjection.cs` — replace its full contents:
```csharp
using AssetTracker.Application.Interfaces;
using AssetTracker.Infrastructure.Data;
using AssetTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");

        services.AddDbContext<AssetTrackerDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IDeviceRepository>(sp =>
            new DeviceRepository(connectionString, sp.GetRequiredService<AssetTrackerDbContext>()));

        return services;
    }
}
```

- [ ] **Step 7: Generate migration for the stored procedures**

EF migrations can execute raw SQL via `migrationBuilder.Sql(...)`. Generate an empty migration, then fill its `Up()`/`Down()` by hand (this is a script-carrying migration, not a schema-diff one):

```bash
cd backend
dotnet ef migrations add AddDeviceStoredProcedures \
  --project AssetTracker.Infrastructure \
  --startup-project AssetTracker.Api \
  --output-dir Data/Migrations
```

Open the generated `backend/AssetTracker.Infrastructure/Data/Migrations/<timestamp>_AddDeviceStoredProcedures.cs` and replace its `Up`/`Down` bodies:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(
        Path.GetDirectoryName(typeof(AddDeviceStoredProcedures).Assembly.Location)!,
        "..", "..", "..", "Data", "StoredProcedures", "usp_Device_Register.sql")));

    migrationBuilder.Sql(File.ReadAllText(Path.Combine(
        Path.GetDirectoryName(typeof(AddDeviceStoredProcedures).Assembly.Location)!,
        "..", "..", "..", "Data", "StoredProcedures", "usp_Device_GetByApiKeyHash.sql")));
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Device_Register;");
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Device_GetByApiKeyHash;");
}
```

This relative-path-from-assembly-location approach is fragile across build configurations. Simpler and more reliable: mark the `.sql` files to copy to the output directory and read them from `AppContext.BaseDirectory` instead. Add to `backend/AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj` (inside the existing `<Project>` element, as a new `<ItemGroup>`):
```xml
<ItemGroup>
  <None Include="Data\StoredProcedures\*.sql" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

And use this in the migration instead:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Device_Register.sql")));
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Device_GetByApiKeyHash.sql")));
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Device_Register;");
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Device_GetByApiKeyHash;");
}
```

This pattern (copy `.sql` to output, read via `AppContext.BaseDirectory` in the migration) is reused unchanged in Tasks 5 and 6 — only the filenames change.

- [ ] **Step 8: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~DeviceRepositoryTests"`
Expected: PASS (4 tests)

- [ ] **Step 9: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Infrastructure backend/AssetTracker.Application backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: device stored procedures + DeviceRepository

usp_Device_Register and usp_Device_GetByApiKeyHash, called via Dapper;
GetByDeviceIdAsync stays a plain EF Core read per the hybrid data-access
design.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 5: Location Stored Procedures + LocationRepository

**Files:**
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/LocationTableType.sql`
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_Insert.sql`
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_BatchInsert.sql`
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_GetLatestByDevice.sql`
- Create: `backend/AssetTracker.Application/Interfaces/ILocationRepository.cs`
- Create: `backend/AssetTracker.Infrastructure/Repositories/LocationRepository.cs`
- Create: `backend/AssetTracker.Tests/Integration/LocationRepositoryTests.cs`
- Modify: `backend/AssetTracker.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `Device` (Task 2), `Location.Reconstitute` (Task 2)
- Produces: `ILocationRepository { Task<Location> InsertAsync(Location location, CancellationToken ct); Task<IReadOnlyList<Location>> BatchInsertAsync(IReadOnlyList<Location> locations, CancellationToken ct); Task<IReadOnlyList<Location>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct); }`

`BatchInsertAsync` uses a SQL Server **table-valued parameter** (TVP) — the idiomatic way to bulk-insert via a stored procedure instead of one round-trip per row. All items in a batch share one device, so the repository reads `DeviceFk` off `locations[0]`.

`GetLatestByDeviceAsync` returns the single most recent location as a 0-or-1-item list, matching the original spec's `list[LocationRead]` response shape for "latest per device."

- [ ] **Step 1: Write the stored procedure scripts**

Write `backend/AssetTracker.Infrastructure/Data/StoredProcedures/LocationTableType.sql`:
```sql
CREATE TYPE LocationTableType AS TABLE
(
    [timestamp]      DATETIMEOFFSET NOT NULL,
    latitude         FLOAT NOT NULL,
    longitude        FLOAT NOT NULL,
    altitude         FLOAT NULL,
    speed            FLOAT NULL,
    satellites       TINYINT NULL,
    hdop             FLOAT NULL,
    battery_voltage  FLOAT NULL,
    is_stale         BIT NOT NULL
);
```

Write `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_Insert.sql`:
```sql
CREATE OR ALTER PROCEDURE usp_Location_Insert
    @DeviceFk INT,
    @Timestamp DATETIMEOFFSET,
    @Latitude FLOAT,
    @Longitude FLOAT,
    @Altitude FLOAT = NULL,
    @Speed FLOAT = NULL,
    @Satellites TINYINT = NULL,
    @Hdop FLOAT = NULL,
    @BatteryVoltage FLOAT = NULL,
    @IsStale BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO locations
        (device_fk, [timestamp], latitude, longitude, altitude, speed, satellites, hdop, battery_voltage, is_stale, created_at)
    OUTPUT
        INSERTED.id AS Id,
        INSERTED.device_fk AS DeviceFk,
        INSERTED.[timestamp] AS Timestamp,
        INSERTED.latitude AS Latitude,
        INSERTED.longitude AS Longitude,
        INSERTED.altitude AS Altitude,
        INSERTED.speed AS Speed,
        INSERTED.satellites AS Satellites,
        INSERTED.hdop AS Hdop,
        INSERTED.battery_voltage AS BatteryVoltage,
        INSERTED.is_stale AS IsStale,
        INSERTED.created_at AS CreatedAt
    VALUES (@DeviceFk, @Timestamp, @Latitude, @Longitude, @Altitude, @Speed, @Satellites, @Hdop, @BatteryVoltage, @IsStale, SYSUTCDATETIME());
END
```

Write `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_BatchInsert.sql`:
```sql
CREATE OR ALTER PROCEDURE usp_Location_BatchInsert
    @DeviceFk INT,
    @Locations LocationTableType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO locations
        (device_fk, [timestamp], latitude, longitude, altitude, speed, satellites, hdop, battery_voltage, is_stale, created_at)
    OUTPUT
        INSERTED.id AS Id,
        INSERTED.device_fk AS DeviceFk,
        INSERTED.[timestamp] AS Timestamp,
        INSERTED.latitude AS Latitude,
        INSERTED.longitude AS Longitude,
        INSERTED.altitude AS Altitude,
        INSERTED.speed AS Speed,
        INSERTED.satellites AS Satellites,
        INSERTED.hdop AS Hdop,
        INSERTED.battery_voltage AS BatteryVoltage,
        INSERTED.is_stale AS IsStale,
        INSERTED.created_at AS CreatedAt
    SELECT @DeviceFk, [timestamp], latitude, longitude, altitude, speed, satellites, hdop, battery_voltage, is_stale, SYSUTCDATETIME()
    FROM @Locations;
END
```

Write `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_GetLatestByDevice.sql`:
```sql
CREATE OR ALTER PROCEDURE usp_Location_GetLatestByDevice
    @DeviceId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        l.id AS Id,
        l.device_fk AS DeviceFk,
        l.[timestamp] AS Timestamp,
        l.latitude AS Latitude,
        l.longitude AS Longitude,
        l.altitude AS Altitude,
        l.speed AS Speed,
        l.satellites AS Satellites,
        l.hdop AS Hdop,
        l.battery_voltage AS BatteryVoltage,
        l.is_stale AS IsStale,
        l.created_at AS CreatedAt
    FROM locations l
    INNER JOIN devices d ON d.id = l.device_fk
    WHERE d.device_id = @DeviceId
    ORDER BY l.[timestamp] DESC;
END
```

`LocationTableType.sql` must be applied in the migration **before** `usp_Location_BatchInsert.sql` (the proc references the type) — Step 4 below orders it first.

- [ ] **Step 2: Write `ILocationRepository` and the failing integration test**

Write `backend/AssetTracker.Application/Interfaces/ILocationRepository.cs`:
```csharp
using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface ILocationRepository
{
    Task<Location> InsertAsync(Location location, CancellationToken ct);
    Task<IReadOnlyList<Location>> BatchInsertAsync(IReadOnlyList<Location> locations, CancellationToken ct);
    Task<IReadOnlyList<Location>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
}
```

Write `backend/AssetTracker.Tests/Integration/LocationRepositoryTests.cs`:
```csharp
using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class LocationRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public LocationRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Device> RegisterDeviceAsync()
    {
        var deviceRepository = new DeviceRepository(_fixture.ConnectionString, _fixture.CreateDbContext());
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);
        return await deviceRepository.RegisterAsync($"test-device-{Guid.NewGuid():N}", apiKeyHash, null, CancellationToken.None);
    }

    [Fact]
    public async Task InsertAsync_ReturnsPersistedLocationWithId()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        var location = new Location(device.Id, DateTimeOffset.UtcNow, 1.5, 2.5, 10, 0.5, 8, 0.9, 3.9, false);

        var saved = await repository.InsertAsync(location, CancellationToken.None);

        Assert.True(saved.Id > 0);
        Assert.Equal(device.Id, saved.DeviceFk);
        Assert.Equal(1.5, saved.Latitude);
    }

    [Fact]
    public async Task BatchInsertAsync_InsertsAllRowsForSameDevice()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        var locations = new List<Location>
        {
            new(device.Id, DateTimeOffset.UtcNow.AddMinutes(-2), 1, 1, null, null, null, null, null, false),
            new(device.Id, DateTimeOffset.UtcNow.AddMinutes(-1), 2, 2, null, null, null, null, null, false),
            new(device.Id, DateTimeOffset.UtcNow, 3, 3, null, null, null, null, null, true)
        };

        var saved = await repository.BatchInsertAsync(locations, CancellationToken.None);

        Assert.Equal(3, saved.Count);
        Assert.All(saved, l => Assert.Equal(device.Id, l.DeviceFk));
        Assert.All(saved, l => Assert.True(l.Id > 0));
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_ReturnsOnlyMostRecentLocation()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        await repository.InsertAsync(new Location(device.Id, DateTimeOffset.UtcNow.AddMinutes(-5), 1, 1, null, null, null, null, null, false), CancellationToken.None);
        var newest = await repository.InsertAsync(new Location(device.Id, DateTimeOffset.UtcNow, 9, 9, null, null, null, null, null, false), CancellationToken.None);

        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(newest.Id, result[0].Id);
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_WithNoLocations_ReturnsEmptyList()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);

        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.Empty(result);
    }
}
```

- [ ] **Step 3: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~LocationRepositoryTests"`
Expected: FAIL to build — `LocationRepository` doesn't exist yet.

- [ ] **Step 4: Implement `LocationRepository`**

Write `backend/AssetTracker.Infrastructure/Repositories/LocationRepository.cs`:
```csharp
using System.Data;
using AssetTracker.Application.Interfaces;
using AssetTracker.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AssetTracker.Infrastructure.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly string _connectionString;

    public LocationRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Location> InsertAsync(Location location, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleAsync<LocationRow>(
            new CommandDefinition(
                "usp_Location_Insert",
                new
                {
                    location.DeviceFk,
                    location.Timestamp,
                    location.Latitude,
                    location.Longitude,
                    location.Altitude,
                    location.Speed,
                    location.Satellites,
                    location.Hdop,
                    location.BatteryVoltage,
                    location.IsStale
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return row.ToEntity();
    }

    public async Task<IReadOnlyList<Location>> BatchInsertAsync(IReadOnlyList<Location> locations, CancellationToken ct)
    {
        var deviceFk = locations[0].DeviceFk;

        var table = new DataTable();
        table.Columns.Add("timestamp", typeof(DateTimeOffset));
        table.Columns.Add("latitude", typeof(double));
        table.Columns.Add("longitude", typeof(double));
        table.Columns.Add("altitude", typeof(double));
        table.Columns.Add("speed", typeof(double));
        table.Columns.Add("satellites", typeof(byte));
        table.Columns.Add("hdop", typeof(double));
        table.Columns.Add("battery_voltage", typeof(double));
        table.Columns.Add("is_stale", typeof(bool));

        foreach (var location in locations)
        {
            table.Rows.Add(
                location.Timestamp,
                location.Latitude,
                location.Longitude,
                (object?)location.Altitude ?? DBNull.Value,
                (object?)location.Speed ?? DBNull.Value,
                (object?)location.Satellites ?? DBNull.Value,
                (object?)location.Hdop ?? DBNull.Value,
                (object?)location.BatteryVoltage ?? DBNull.Value,
                location.IsStale);
        }

        var parameters = new DynamicParameters();
        parameters.Add("DeviceFk", deviceFk);
        parameters.Add("Locations", table.AsTableValuedParameter("LocationTableType"));

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<LocationRow>(
            new CommandDefinition(
                "usp_Location_BatchInsert",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return rows.Select(r => r.ToEntity()).ToList();
    }

    public async Task<IReadOnlyList<Location>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<LocationRow>(
            new CommandDefinition(
                "usp_Location_GetLatestByDevice",
                new { DeviceId = deviceId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return row is null ? Array.Empty<Location>() : new[] { row.ToEntity() };
    }

    private sealed class LocationRow
    {
        public long Id { get; set; }
        public int DeviceFk { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public double? Speed { get; set; }
        public byte? Satellites { get; set; }
        public double? Hdop { get; set; }
        public double? BatteryVoltage { get; set; }
        public bool IsStale { get; set; }
        public DateTime CreatedAt { get; set; }

        public Location ToEntity() => Location.Reconstitute(
            Id, DeviceFk, Timestamp, Latitude, Longitude, Altitude, Speed, Satellites, Hdop, BatteryVoltage, IsStale, CreatedAt);
    }
}
```

- [ ] **Step 5: Register in DI**

Modify `backend/AssetTracker.Infrastructure/DependencyInjection.cs` — add one line inside `AddInfrastructure`, right after the `IDeviceRepository` registration:
```csharp
        services.AddScoped<ILocationRepository>(_ => new LocationRepository(connectionString));
```

No new `using` is needed beyond what's already there. Full resulting file:
```csharp
using AssetTracker.Application.Interfaces;
using AssetTracker.Infrastructure.Data;
using AssetTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");

        services.AddDbContext<AssetTrackerDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IDeviceRepository>(sp =>
            new DeviceRepository(connectionString, sp.GetRequiredService<AssetTrackerDbContext>()));
        services.AddScoped<ILocationRepository>(_ => new LocationRepository(connectionString));

        return services;
    }
}
```

- [ ] **Step 6: Generate migration for the location stored procedures**

```bash
cd backend
dotnet ef migrations add AddLocationStoredProcedures \
  --project AssetTracker.Infrastructure \
  --startup-project AssetTracker.Api \
  --output-dir Data/Migrations
```

Fill in `Up`/`Down` (same `AppContext.BaseDirectory` pattern as Task 4 — `LocationTableType.sql` must run first since the batch-insert proc depends on the type):
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "LocationTableType.sql")));
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Location_Insert.sql")));
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Location_BatchInsert.sql")));
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Location_GetLatestByDevice.sql")));
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Location_GetLatestByDevice;");
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Location_BatchInsert;");
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Location_Insert;");
    migrationBuilder.Sql("DROP TYPE IF EXISTS LocationTableType;");
}
```

- [ ] **Step 7: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~LocationRepositoryTests"`
Expected: PASS (4 tests)

- [ ] **Step 8: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Infrastructure backend/AssetTracker.Application backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: location stored procedures + LocationRepository

usp_Location_Insert, usp_Location_BatchInsert (table-valued parameter),
and usp_Location_GetLatestByDevice, called via Dapper.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 6: Retention Stored Procedure + RetentionRepository

**Files:**
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Retention_PurgeOldLocations.sql`
- Create: `backend/AssetTracker.Application/Interfaces/IRetentionRepository.cs`
- Create: `backend/AssetTracker.Infrastructure/Repositories/RetentionRepository.cs`
- Create: `backend/AssetTracker.Tests/Integration/RetentionRepositoryTests.cs`
- Modify: `backend/AssetTracker.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `LocationRepository` (Task 5, for test setup only)
- Produces: `IRetentionRepository { Task<int> PurgeOldLocationsAsync(int retentionDays, CancellationToken ct); }`

This closes out the 30-day retention requirement from the spec. Per the design doc's Open TBDs (§11), **wiring this into a scheduled job is out of scope for this plan** — this task builds and proves the repository/procedure only; a future task picks a scheduling mechanism (SQL Agent job vs. a hosted `BackgroundService`) and calls `IRetentionRepository.PurgeOldLocationsAsync` from it.

- [ ] **Step 1: Write the stored procedure**

Write `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Retention_PurgeOldLocations.sql`:
```sql
CREATE OR ALTER PROCEDURE usp_Retention_PurgeOldLocations
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Cutoff DATETIMEOFFSET = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());
    DECLARE @DeletedCount INT;

    DELETE FROM locations WHERE [timestamp] < @Cutoff;
    SET @DeletedCount = @@ROWCOUNT;

    SELECT @DeletedCount AS DeletedCount;
END
```

- [ ] **Step 2: Write `IRetentionRepository` and the failing integration test**

Write `backend/AssetTracker.Application/Interfaces/IRetentionRepository.cs`:
```csharp
namespace AssetTracker.Application.Interfaces;

public interface IRetentionRepository
{
    Task<int> PurgeOldLocationsAsync(int retentionDays, CancellationToken ct);
}
```

Write `backend/AssetTracker.Tests/Integration/RetentionRepositoryTests.cs`:
```csharp
using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class RetentionRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public RetentionRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PurgeOldLocationsAsync_DeletesOnlyLocationsOlderThanRetentionWindow()
    {
        var deviceRepository = new DeviceRepository(_fixture.ConnectionString, _fixture.CreateDbContext());
        var apiKeyHash = new byte[32];
        Random.Shared.NextBytes(apiKeyHash);
        var device = await deviceRepository.RegisterAsync($"test-device-{Guid.NewGuid():N}", apiKeyHash, null, CancellationToken.None);

        var locationRepository = new LocationRepository(_fixture.ConnectionString);
        var oldLocation = new Location(device.Id, DateTimeOffset.UtcNow.AddDays(-40), 1, 1, null, null, null, null, null, false);
        var recentLocation = new Location(device.Id, DateTimeOffset.UtcNow.AddDays(-1), 2, 2, null, null, null, null, null, false);
        await locationRepository.InsertAsync(oldLocation, CancellationToken.None);
        var savedRecent = await locationRepository.InsertAsync(recentLocation, CancellationToken.None);

        var retentionRepository = new RetentionRepository(_fixture.ConnectionString);
        var deletedCount = await retentionRepository.PurgeOldLocationsAsync(30, CancellationToken.None);

        Assert.Equal(1, deletedCount);
        var remaining = await locationRepository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);
        Assert.Single(remaining);
        Assert.Equal(savedRecent.Id, remaining[0].Id);
    }
}
```

- [ ] **Step 3: Run test, verify it fails to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~RetentionRepositoryTests"`
Expected: FAIL to build — `RetentionRepository` doesn't exist yet.

- [ ] **Step 4: Implement `RetentionRepository`**

Write `backend/AssetTracker.Infrastructure/Repositories/RetentionRepository.cs`:
```csharp
using System.Data;
using AssetTracker.Application.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace AssetTracker.Infrastructure.Repositories;

public class RetentionRepository : IRetentionRepository
{
    private readonly string _connectionString;

    public RetentionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<int> PurgeOldLocationsAsync(int retentionDays, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        return await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                "usp_Retention_PurgeOldLocations",
                new { RetentionDays = retentionDays },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }
}
```

- [ ] **Step 5: Register in DI**

Modify `backend/AssetTracker.Infrastructure/DependencyInjection.cs` — add one line after the `ILocationRepository` registration:
```csharp
        services.AddScoped<IRetentionRepository>(_ => new RetentionRepository(connectionString));
```

- [ ] **Step 6: Generate migration**

```bash
cd backend
dotnet ef migrations add AddRetentionStoredProcedure \
  --project AssetTracker.Infrastructure \
  --startup-project AssetTracker.Api \
  --output-dir Data/Migrations
```

Fill in `Up`/`Down`:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Retention_PurgeOldLocations.sql")));
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Retention_PurgeOldLocations;");
}
```

- [ ] **Step 7: Run test, verify it passes**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~RetentionRepositoryTests"`
Expected: PASS

- [ ] **Step 8: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Infrastructure backend/AssetTracker.Application backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: retention stored procedure + RetentionRepository

usp_Retention_PurgeOldLocations enforces the 30-day rolling window.
Scheduling mechanism is intentionally out of scope here (open TBD from
the design doc) — this proves the repository/procedure in isolation.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 7: AdminUser EF Repository + Seed Migration

**Files:**
- Create: `backend/AssetTracker.Application/Interfaces/IAdminUserRepository.cs`
- Create: `backend/AssetTracker.Infrastructure/Repositories/AdminUserRepository.cs`
- Create: `backend/AssetTracker.Tests/Integration/AdminUserRepositoryTests.cs`
- Modify: `backend/AssetTracker.Infrastructure/Data/AssetTrackerDbContext.cs` (add `HasData` seed)
- Modify: `backend/AssetTracker.Infrastructure/DependencyInjection.cs`
- Modify: `backend/AssetTracker.Tests/AssetTracker.Tests.csproj` (add BCrypt.Net-Next, for test-side verification only)

**Interfaces:**
- Consumes: `AssetTrackerDbContext` (Task 3), `AdminUser` (Task 2)
- Produces: `IAdminUserRepository { Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct); }`

There's no admin-registration endpoint in this design (see the approved design doc's endpoint table — only `POST /api/v1/auth/login` exists for admins). One admin user is seeded via migration instead. `AdminUser` is read straight through EF Core (private setters are fine — EF materializes via backing fields, bypassing constructor/property access rules entirely), so unlike `Device`/`Location` it needs no `Reconstitute` factory.

**Seeded credential — dev only:** username `admin`, password `ChangeMe123!`. The hash below was generated for real with `BCrypt.Net-Next` 4.2.0 at work factor 11 (not invented) — `BCrypt.Net.BCrypt.Verify("ChangeMe123!", hash)` returns `true` against it. This must be rotated or removed before any real deployment; it exists purely so the login endpoint has something to authenticate against in this portfolio project.

- [ ] **Step 1: Add BCrypt.Net-Next to the test project**

```bash
cd backend
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj package BCrypt.Net-Next --version 4.2.0
```

- [ ] **Step 2: Write `IAdminUserRepository` and the failing integration test**

Write `backend/AssetTracker.Application/Interfaces/IAdminUserRepository.cs`:
```csharp
using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct);
}
```

Write `backend/AssetTracker.Tests/Integration/AdminUserRepositoryTests.cs`:
```csharp
using AssetTracker.Infrastructure.Repositories;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class AdminUserRepositoryTests
{
    private readonly SqlServerFixture _fixture;

    public AdminUserRepositoryTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByUsernameAsync_WithSeededAdmin_ReturnsUserWithValidPasswordHash()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new AdminUserRepository(dbContext);

        var user = await repository.GetByUsernameAsync("admin", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("admin", user!.Username);
        Assert.True(BCrypt.Net.BCrypt.Verify("ChangeMe123!", user.PasswordHash));
    }

    [Fact]
    public async Task GetByUsernameAsync_WithUnknownUsername_ReturnsNull()
    {
        await using var dbContext = _fixture.CreateDbContext();
        var repository = new AdminUserRepository(dbContext);

        var user = await repository.GetByUsernameAsync("does-not-exist", CancellationToken.None);

        Assert.Null(user);
    }
}
```

- [ ] **Step 3: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~AdminUserRepositoryTests"`
Expected: FAIL to build — `AdminUserRepository` doesn't exist yet.

- [ ] **Step 4: Implement `AdminUserRepository`**

Write `backend/AssetTracker.Infrastructure/Repositories/AdminUserRepository.cs`:
```csharp
using AssetTracker.Application.Interfaces;
using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.Infrastructure.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly AssetTrackerDbContext _dbContext;

    public AdminUserRepository(AssetTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        return await _dbContext.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Username == username, ct);
    }
}
```

- [ ] **Step 5: Add the seed to `AssetTrackerDbContext`**

Modify `backend/AssetTracker.Infrastructure/Data/AssetTrackerDbContext.cs` — inside the `modelBuilder.Entity<AdminUser>(entity => { ... })` block, add one line right before its closing `});`:
```csharp
            entity.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasData(new
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "$2a$11$w12C1tmcv4IC7YmfNIm9sOhwTrLehZMio3BmNDNKmrG/iDDu2RstC",
                CreatedAt = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc)
            });
        });
```

- [ ] **Step 6: Register in DI**

Modify `backend/AssetTracker.Infrastructure/DependencyInjection.cs` — add one line after the `IRetentionRepository` registration:
```csharp
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
```

- [ ] **Step 7: Generate the seed migration**

```bash
cd backend
dotnet ef migrations add SeedAdminUser \
  --project AssetTracker.Infrastructure \
  --startup-project AssetTracker.Api \
  --output-dir Data/Migrations
```

This one is a genuine schema-diff migration (EF detects the new `HasData` seed row against the model snapshot and generates the `InsertData` call itself) — no manual `Up`/`Down` editing needed here, unlike Tasks 4–6.

- [ ] **Step 8: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~AdminUserRepositoryTests"`
Expected: PASS (2 tests)

- [ ] **Step 9: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Infrastructure backend/AssetTracker.Application backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: AdminUser EF repository + seeded dev admin user

Plain EF Core read (no stored procedure — matches the hybrid design's
"simple CRUD" bucket). Seeds one dev-only admin/ChangeMe123! credential
via migration since there's no admin-registration endpoint.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 8: DTOs + Validation

**Files:**
- Create: `backend/AssetTracker.Application/Dtos/LocationCreateDto.cs`
- Create: `backend/AssetTracker.Application/Dtos/LocationBatchCreateDto.cs`
- Create: `backend/AssetTracker.Application/Dtos/LocationReadDto.cs`
- Create: `backend/AssetTracker.Application/Dtos/LocationCreateResponseDto.cs`
- Create: `backend/AssetTracker.Application/Dtos/DeviceRegisterRequestDto.cs`
- Create: `backend/AssetTracker.Application/Dtos/DeviceRegisterResponseDto.cs`
- Create: `backend/AssetTracker.Application/Dtos/LoginRequestDto.cs`
- Create: `backend/AssetTracker.Application/Dtos/LoginResponseDto.cs`
- Create: `backend/AssetTracker.Tests/Unit/Dtos/LocationCreateDtoValidationTests.cs`
- Create: `backend/AssetTracker.Tests/Unit/Dtos/LocationBatchCreateDtoValidationTests.cs`
- Create: `backend/AssetTracker.Tests/Unit/Dtos/DeviceRegisterRequestDtoValidationTests.cs`
- Create: `backend/AssetTracker.Tests/Unit/Dtos/LoginRequestDtoValidationTests.cs`

**Interfaces:**
- Produces: all DTO types listed above, validated via `System.ComponentModel.DataAnnotations` (built into the BCL — no package needed). JSON serialization uses ASP.NET Core's default camelCase policy, so no `[JsonPropertyName]` attributes are needed anywhere.

Output-only DTOs (`LocationReadDto`, `LocationCreateResponseDto`, `DeviceRegisterResponseDto`, `LoginResponseDto`) carry no validation attributes — they're never model-bound from a request, so there's nothing to validate.

- [ ] **Step 1: Write the failing validation tests**

Write `backend/AssetTracker.Tests/Unit/Dtos/LocationCreateDtoValidationTests.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class LocationCreateDtoValidationTests
{
    private static IList<ValidationResult> Validate(LocationCreateDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    private static LocationCreateDto ValidDto() => new()
    {
        DeviceId = "goat-001",
        Timestamp = DateTimeOffset.UtcNow,
        Latitude = -31.4231,
        Longitude = -62.0834
    };

    [Fact]
    public void Validate_WithValidDto_ReturnsNoErrors()
    {
        Assert.Empty(Validate(ValidDto()));
    }

    [Fact]
    public void Validate_WithEmptyDeviceId_ReturnsError()
    {
        var dto = ValidDto();
        dto.DeviceId = "";

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationCreateDto.DeviceId)));
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Validate_WithLatitudeOutOfRange_ReturnsError(double latitude)
    {
        var dto = ValidDto();
        dto.Latitude = latitude;

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationCreateDto.Latitude)));
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Validate_WithLongitudeOutOfRange_ReturnsError(double longitude)
    {
        var dto = ValidDto();
        dto.Longitude = longitude;

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationCreateDto.Longitude)));
    }

    [Fact]
    public void Validate_WithOptionalFieldsNull_ReturnsNoErrors()
    {
        var dto = ValidDto();
        dto.Altitude = null;
        dto.Speed = null;
        dto.Satellites = null;
        dto.Hdop = null;
        dto.BatteryVoltage = null;

        Assert.Empty(Validate(dto));
    }
}
```

Write `backend/AssetTracker.Tests/Unit/Dtos/LocationBatchCreateDtoValidationTests.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class LocationBatchCreateDtoValidationTests
{
    private static IList<ValidationResult> Validate(LocationBatchCreateDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_WithEmptyLocationsList_ReturnsError()
    {
        var dto = new LocationBatchCreateDto { DeviceId = "goat-001", Locations = new List<LocationCreateDto>() };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LocationBatchCreateDto.Locations)));
    }

    [Fact]
    public void Validate_WithAtLeastOneLocation_ReturnsNoErrors()
    {
        var dto = new LocationBatchCreateDto
        {
            DeviceId = "goat-001",
            Locations = new List<LocationCreateDto>
            {
                new() { DeviceId = "goat-001", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 }
            }
        };

        Assert.Empty(Validate(dto));
    }
}
```

Write `backend/AssetTracker.Tests/Unit/Dtos/DeviceRegisterRequestDtoValidationTests.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class DeviceRegisterRequestDtoValidationTests
{
    private static IList<ValidationResult> Validate(DeviceRegisterRequestDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_WithEmptyDeviceId_ReturnsError()
    {
        var dto = new DeviceRegisterRequestDto { DeviceId = "" };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(DeviceRegisterRequestDto.DeviceId)));
    }

    [Fact]
    public void Validate_WithDeviceIdAndNoDisplayName_ReturnsNoErrors()
    {
        var dto = new DeviceRegisterRequestDto { DeviceId = "goat-001" };

        Assert.Empty(Validate(dto));
    }
}
```

Write `backend/AssetTracker.Tests/Unit/Dtos/LoginRequestDtoValidationTests.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Unit.Dtos;

public class LoginRequestDtoValidationTests
{
    private static IList<ValidationResult> Validate(LoginRequestDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_WithEmptyUsername_ReturnsError()
    {
        var dto = new LoginRequestDto { Username = "", Password = "something" };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LoginRequestDto.Username)));
    }

    [Fact]
    public void Validate_WithEmptyPassword_ReturnsError()
    {
        var dto = new LoginRequestDto { Username = "admin", Password = "" };

        Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(LoginRequestDto.Password)));
    }

    [Fact]
    public void Validate_WithBothFieldsSet_ReturnsNoErrors()
    {
        var dto = new LoginRequestDto { Username = "admin", Password = "something" };

        Assert.Empty(Validate(dto));
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~Unit.Dtos"`
Expected: FAIL to build — none of the DTO types exist yet.

- [ ] **Step 3: Implement the DTOs**

Write `backend/AssetTracker.Application/Dtos/LocationCreateDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Application.Dtos;

public class LocationCreateDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset Timestamp { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public byte? Satellites { get; set; }
    public double? Hdop { get; set; }
    public double? BatteryVoltage { get; set; }
    public bool IsStale { get; set; }
}
```

Write `backend/AssetTracker.Application/Dtos/LocationBatchCreateDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Application.Dtos;

public class LocationBatchCreateDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [MinLength(1, ErrorMessage = "At least one location is required.")]
    public List<LocationCreateDto> Locations { get; set; } = new();
}
```

Write `backend/AssetTracker.Application/Dtos/LocationReadDto.cs`:
```csharp
namespace AssetTracker.Application.Dtos;

public class LocationReadDto
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public byte? Satellites { get; set; }
    public double? Hdop { get; set; }
    public double? BatteryVoltage { get; set; }
    public bool IsStale { get; set; }
}
```

Write `backend/AssetTracker.Application/Dtos/LocationCreateResponseDto.cs`:
```csharp
namespace AssetTracker.Application.Dtos;

public class LocationCreateResponseDto
{
    public long Id { get; set; }
    public string Status { get; set; } = "accepted";
}
```

Write `backend/AssetTracker.Application/Dtos/DeviceRegisterRequestDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Application.Dtos;

public class DeviceRegisterRequestDto
{
    [Required]
    public string DeviceId { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
```

Write `backend/AssetTracker.Application/Dtos/DeviceRegisterResponseDto.cs`:
```csharp
namespace AssetTracker.Application.Dtos;

public class DeviceRegisterResponseDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
```

Write `backend/AssetTracker.Application/Dtos/LoginRequestDto.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.Application.Dtos;

public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

Write `backend/AssetTracker.Application/Dtos/LoginResponseDto.cs`:
```csharp
namespace AssetTracker.Application.Dtos;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~Unit.Dtos"`
Expected: PASS (13 tests)

- [ ] **Step 5: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Application backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: Application DTOs with DataAnnotations validation

Request/response contracts matching the approved design's API shapes
(camelCase over the wire via ASP.NET Core's default JSON policy).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 9: Password Hashing + JWT Token Generator

**Files:**
- Create: `backend/AssetTracker.Application/Interfaces/IPasswordHasher.cs`
- Create: `backend/AssetTracker.Application/Interfaces/IJwtTokenGenerator.cs`
- Create: `backend/AssetTracker.Infrastructure/Security/JwtOptions.cs`
- Create: `backend/AssetTracker.Infrastructure/Security/BCryptPasswordHasher.cs`
- Create: `backend/AssetTracker.Infrastructure/Security/JwtTokenGenerator.cs`
- Create: `backend/AssetTracker.Tests/Unit/Security/BCryptPasswordHasherTests.cs`
- Create: `backend/AssetTracker.Tests/Unit/Security/JwtTokenGeneratorTests.cs`
- Modify: `backend/AssetTracker.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `AdminUser` (Task 2)
- Produces: `IPasswordHasher { string Hash(string password); bool Verify(string password, string hash); }`, `IJwtTokenGenerator { string GenerateToken(AdminUser user); }`, `JwtOptions { string Key, Issuer, Audience; int ExpiryMinutes; }`

- [ ] **Step 1: Add packages to Infrastructure**

```bash
cd backend
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package BCrypt.Net-Next --version 4.2.0
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package System.IdentityModel.Tokens.Jwt --version 8.22.0
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Microsoft.IdentityModel.Tokens --version 8.22.0
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Microsoft.Extensions.Options --version 10.0.11
dotnet add AssetTracker.Infrastructure/AssetTracker.Infrastructure.csproj package Microsoft.Extensions.Options.ConfigurationExtensions --version 10.0.11
```

- [ ] **Step 2: Write the interfaces and failing unit tests**

Write `backend/AssetTracker.Application/Interfaces/IPasswordHasher.cs`:
```csharp
namespace AssetTracker.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
```

Write `backend/AssetTracker.Application/Interfaces/IJwtTokenGenerator.cs`:
```csharp
using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(AdminUser user);
}
```

Write `backend/AssetTracker.Tests/Unit/Security/BCryptPasswordHasherTests.cs`:
```csharp
using AssetTracker.Infrastructure.Security;
using Xunit;

namespace AssetTracker.Tests.Unit.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple");

        Assert.True(_hasher.Verify("CorrectHorseBatteryStaple", hash));
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("CorrectHorseBatteryStaple");

        Assert.False(_hasher.Verify("WrongPassword", hash));
    }
}
```

Write `backend/AssetTracker.Tests/Unit/Security/JwtTokenGeneratorTests.cs`:
```csharp
using AssetTracker.Domain.Entities;
using AssetTracker.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Xunit;

namespace AssetTracker.Tests.Unit.Security;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ReturnsThreePartToken_ForValidUser()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "a-test-signing-key-that-is-at-least-32-bytes-long",
            Issuer = "AssetTrackerApi",
            Audience = "AssetTrackerDashboard",
            ExpiryMinutes = 60
        });
        var generator = new JwtTokenGenerator(options);
        var user = new AdminUser("admin", "hashed-password");

        var token = generator.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length);
    }
}
```

- [ ] **Step 3: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~Unit.Security"`
Expected: FAIL to build — `BCryptPasswordHasher`/`JwtTokenGenerator`/`JwtOptions` don't exist yet.

- [ ] **Step 4: Implement the security classes**

Write `backend/AssetTracker.Infrastructure/Security/JwtOptions.cs`:
```csharp
namespace AssetTracker.Infrastructure.Security;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}
```

Write `backend/AssetTracker.Infrastructure/Security/BCryptPasswordHasher.cs`:
```csharp
using AssetTracker.Application.Interfaces;

namespace AssetTracker.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
```

Write `backend/AssetTracker.Infrastructure/Security/JwtTokenGenerator.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssetTracker.Application.Interfaces;
using AssetTracker.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AssetTracker.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(AdminUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

- [ ] **Step 5: Register in DI**

Modify `backend/AssetTracker.Infrastructure/DependencyInjection.cs` — replace its full contents (adds the `Jwt` options binding and the two new registrations):
```csharp
using AssetTracker.Application.Interfaces;
using AssetTracker.Infrastructure.Data;
using AssetTracker.Infrastructure.Repositories;
using AssetTracker.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");

        services.AddDbContext<AssetTrackerDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IDeviceRepository>(sp =>
            new DeviceRepository(connectionString, sp.GetRequiredService<AssetTrackerDbContext>()));
        services.AddScoped<ILocationRepository>(_ => new LocationRepository(connectionString));
        services.AddScoped<IRetentionRepository>(_ => new RetentionRepository(connectionString));
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
```

- [ ] **Step 6: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~Unit.Security"`
Expected: PASS (3 tests)

- [ ] **Step 7: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Infrastructure
git commit -m "$(cat <<'EOF'
RT: BCrypt password hashing + JWT token generation

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 10: AuthService

**Files:**
- Create: `backend/AssetTracker.Application/Exceptions/InvalidCredentialsException.cs`
- Create: `backend/AssetTracker.Application/Services/IAuthService.cs`
- Create: `backend/AssetTracker.Application/Services/AuthService.cs`
- Create: `backend/AssetTracker.Application/DependencyInjection.cs`
- Create: `backend/AssetTracker.Tests/Unit/Services/AuthServiceTests.cs`
- Modify: `backend/AssetTracker.Application/AssetTracker.Application.csproj` (add DI abstractions package)
- Modify: `backend/AssetTracker.Tests/AssetTracker.Tests.csproj` (add Moq)
- Modify: `backend/AssetTracker.Api/Program.cs`

**Interfaces:**
- Consumes: `IAdminUserRepository` (Task 7), `IPasswordHasher`, `IJwtTokenGenerator` (Task 9), `LoginRequestDto`, `LoginResponseDto` (Task 8)
- Produces: `IAuthService { Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct); }`, `AddApplicationServices(this IServiceCollection)`

This is the first task to use a mocking framework — from here on, `Application`-layer services are unit-tested against mocked repository/security interfaces rather than a real database, matching `.clinerules/testing.md`'s intent (only DB-touching code needs the real-DB-no-fakes rule; pure business logic gets mocked collaborators).

- [ ] **Step 1: Add packages**

```bash
cd backend
dotnet add AssetTracker.Application/AssetTracker.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.11
dotnet add AssetTracker.Tests/AssetTracker.Tests.csproj package Moq --version 4.20.72
```

- [ ] **Step 2: Write the exception, interface, and failing unit tests**

Write `backend/AssetTracker.Application/Exceptions/InvalidCredentialsException.cs`:
```csharp
namespace AssetTracker.Application.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException() : base("Invalid username or password.") { }
}
```

Write `backend/AssetTracker.Application/Services/IAuthService.cs`:
```csharp
using AssetTracker.Application.Dtos;

namespace AssetTracker.Application.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct);
}
```

Write `backend/AssetTracker.Tests/Unit/Services/AuthServiceTests.cs`:
```csharp
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Application.Services;
using AssetTracker.Domain.Entities;
using Moq;
using Xunit;

namespace AssetTracker.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly Mock<IAdminUserRepository> _adminUserRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_adminUserRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        var user = new AdminUser("admin", "hashed-password");
        _adminUserRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("correct-password", "hashed-password")).Returns(true);
        _jwtTokenGenerator.Setup(g => g.GenerateToken(user)).Returns("fake-jwt-token");

        var result = await _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "correct-password" }, CancellationToken.None);

        Assert.Equal("fake-jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownUsername_ThrowsInvalidCredentialsException()
    {
        _adminUserRepository.Setup(r => r.GetByUsernameAsync("ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "ghost", Password = "whatever" }, CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        var user = new AdminUser("admin", "hashed-password");
        _adminUserRepository.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong-password", "hashed-password")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Username = "admin", Password = "wrong-password" }, CancellationToken.None));
    }
}
```

- [ ] **Step 3: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests"`
Expected: FAIL to build — `AuthService` doesn't exist yet.

- [ ] **Step 4: Implement `AuthService`**

Write `backend/AssetTracker.Application/Services/AuthService.cs`:
```csharp
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;

namespace AssetTracker.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IAdminUserRepository adminUserRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _adminUserRepository = adminUserRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct)
    {
        var user = await _adminUserRepository.GetByUsernameAsync(request.Username, ct);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var token = _jwtTokenGenerator.GenerateToken(user);
        return new LoginResponseDto { Token = token };
    }
}
```

- [ ] **Step 5: Create the Application DI extension and wire it into `Program.cs`**

Write `backend/AssetTracker.Application/DependencyInjection.cs`:
```csharp
using AssetTracker.Application.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
```

Modify `backend/AssetTracker.Api/Program.cs` — add one line after `builder.Services.AddInfrastructure(builder.Configuration);`:
```csharp
builder.Services.AddApplicationServices();
```

Full resulting file:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

public partial class Program { }
```

- [ ] **Step 6: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~AuthServiceTests"`
Expected: PASS (3 tests)

- [ ] **Step 7: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Application backend/AssetTracker.Api backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: AuthService — admin login business logic

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 11: LocationService + DeviceService

**Files:**
- Create: `backend/AssetTracker.Application/Exceptions/DeviceNotFoundException.cs`
- Create: `backend/AssetTracker.Application/Exceptions/DeviceAlreadyExistsException.cs`
- Create: `backend/AssetTracker.Application/Services/ILocationService.cs`
- Create: `backend/AssetTracker.Application/Services/LocationService.cs`
- Create: `backend/AssetTracker.Application/Services/IDeviceService.cs`
- Create: `backend/AssetTracker.Application/Services/DeviceService.cs`
- Create: `backend/AssetTracker.Tests/Unit/Services/LocationServiceTests.cs`
- Create: `backend/AssetTracker.Tests/Unit/Services/DeviceServiceTests.cs`
- Modify: `backend/AssetTracker.Application/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IDeviceRepository` (Task 4), `ILocationRepository` (Task 5), `Location`/`Device` + `Reconstitute` (Task 2), all `Dtos` (Task 8)
- Produces:
  - `ILocationService { Task<LocationCreateResponseDto> CreateAsync(LocationCreateDto request, CancellationToken ct); Task<IReadOnlyList<LocationCreateResponseDto>> CreateBatchAsync(LocationBatchCreateDto request, CancellationToken ct); Task<IReadOnlyList<LocationReadDto>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct); }`
  - `IDeviceService { Task<DeviceRegisterResponseDto> RegisterAsync(DeviceRegisterRequestDto request, CancellationToken ct); }`

`DeviceService.RegisterAsync` generates the device's API key here: 32 cryptographically random bytes, base64-encoded as the key handed back to the caller (shown once), SHA-256 of the raw bytes stored as `api_key_hash`. Task 12's `ApiKeyAuthenticationHandler` must decode+hash incoming keys the same way to validate them — noted there.

- [ ] **Step 1: Write the exceptions, interfaces, and failing unit tests**

Write `backend/AssetTracker.Application/Exceptions/DeviceNotFoundException.cs`:
```csharp
namespace AssetTracker.Application.Exceptions;

public class DeviceNotFoundException : Exception
{
    public DeviceNotFoundException(string deviceId) : base($"Device '{deviceId}' was not found.") { }
}
```

Write `backend/AssetTracker.Application/Exceptions/DeviceAlreadyExistsException.cs`:
```csharp
namespace AssetTracker.Application.Exceptions;

public class DeviceAlreadyExistsException : Exception
{
    public DeviceAlreadyExistsException(string deviceId) : base($"Device '{deviceId}' already exists.") { }
}
```

Write `backend/AssetTracker.Application/Services/ILocationService.cs`:
```csharp
using AssetTracker.Application.Dtos;

namespace AssetTracker.Application.Services;

public interface ILocationService
{
    Task<LocationCreateResponseDto> CreateAsync(LocationCreateDto request, CancellationToken ct);
    Task<IReadOnlyList<LocationCreateResponseDto>> CreateBatchAsync(LocationBatchCreateDto request, CancellationToken ct);
    Task<IReadOnlyList<LocationReadDto>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
}
```

Write `backend/AssetTracker.Application/Services/IDeviceService.cs`:
```csharp
using AssetTracker.Application.Dtos;

namespace AssetTracker.Application.Services;

public interface IDeviceService
{
    Task<DeviceRegisterResponseDto> RegisterAsync(DeviceRegisterRequestDto request, CancellationToken ct);
}
```

Write `backend/AssetTracker.Tests/Unit/Services/LocationServiceTests.cs`:
```csharp
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Application.Services;
using AssetTracker.Domain.Entities;
using Moq;
using Xunit;

namespace AssetTracker.Tests.Unit.Services;

public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _locationRepository = new();
    private readonly Mock<IDeviceRepository> _deviceRepository = new();
    private readonly LocationService _sut;

    public LocationServiceTests()
    {
        _sut = new LocationService(_locationRepository.Object, _deviceRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownDevice_ThrowsDeviceNotFoundException()
    {
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("ghost-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);

        var request = new LocationCreateDto { DeviceId = "ghost-001", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 };

        await Assert.ThrowsAsync<DeviceNotFoundException>(() => _sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithKnownDevice_ReturnsAcceptedResponse()
    {
        var device = Device.Reconstitute(5, "goat-001", new byte[] { 1 }, null, true, DateTime.UtcNow);
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        var savedLocation = Location.Reconstitute(100, 5, DateTimeOffset.UtcNow, 1, 1, null, null, null, null, null, false, DateTime.UtcNow);
        _locationRepository.Setup(r => r.InsertAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedLocation);

        var request = new LocationCreateDto { DeviceId = "goat-001", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 };

        var result = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal(100, result.Id);
        Assert.Equal("accepted", result.Status);
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_ReturnsMappedDtos()
    {
        var location = Location.Reconstitute(1, 5, DateTimeOffset.UtcNow, 10, 20, null, null, null, null, null, true, DateTime.UtcNow);
        _locationRepository.Setup(r => r.GetLatestByDeviceAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Location> { location });

        var result = await _sut.GetLatestByDeviceAsync("goat-001", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("goat-001", result[0].DeviceId);
        Assert.True(result[0].IsStale);
    }
}
```

Write `backend/AssetTracker.Tests/Unit/Services/DeviceServiceTests.cs`:
```csharp
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Application.Services;
using AssetTracker.Domain.Entities;
using Moq;
using Xunit;

namespace AssetTracker.Tests.Unit.Services;

public class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _deviceRepository = new();
    private readonly DeviceService _sut;

    public DeviceServiceTests()
    {
        _sut = new DeviceService(_deviceRepository.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithNewDeviceId_ReturnsApiKey()
    {
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("goat-003", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);
        _deviceRepository
            .Setup(r => r.RegisterAsync("goat-003", It.IsAny<byte[]>(), "Goat 3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Device.Reconstitute(1, "goat-003", new byte[] { 1 }, "Goat 3", true, DateTime.UtcNow));

        var result = await _sut.RegisterAsync(new DeviceRegisterRequestDto { DeviceId = "goat-003", DisplayName = "Goat 3" }, CancellationToken.None);

        Assert.Equal("goat-003", result.DeviceId);
        Assert.False(string.IsNullOrWhiteSpace(result.ApiKey));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingDeviceId_ThrowsDeviceAlreadyExistsException()
    {
        _deviceRepository.Setup(r => r.GetByDeviceIdAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Device.Reconstitute(1, "goat-001", new byte[] { 1 }, null, true, DateTime.UtcNow));

        await Assert.ThrowsAsync<DeviceAlreadyExistsException>(() =>
            _sut.RegisterAsync(new DeviceRegisterRequestDto { DeviceId = "goat-001" }, CancellationToken.None));
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~LocationServiceTests|FullyQualifiedName~DeviceServiceTests"`
Expected: FAIL to build — `LocationService`/`DeviceService` don't exist yet.

- [ ] **Step 3: Implement the services**

Write `backend/AssetTracker.Application/Services/LocationService.cs`:
```csharp
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;
using AssetTracker.Domain.Entities;

namespace AssetTracker.Application.Services;

public class LocationService : ILocationService
{
    private readonly ILocationRepository _locationRepository;
    private readonly IDeviceRepository _deviceRepository;

    public LocationService(ILocationRepository locationRepository, IDeviceRepository deviceRepository)
    {
        _locationRepository = locationRepository;
        _deviceRepository = deviceRepository;
    }

    public async Task<LocationCreateResponseDto> CreateAsync(LocationCreateDto request, CancellationToken ct)
    {
        var device = await _deviceRepository.GetByDeviceIdAsync(request.DeviceId, ct)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        var location = new Location(
            device.Id, request.Timestamp, request.Latitude, request.Longitude,
            request.Altitude, request.Speed, request.Satellites, request.Hdop,
            request.BatteryVoltage, request.IsStale);

        var saved = await _locationRepository.InsertAsync(location, ct);

        return new LocationCreateResponseDto { Id = saved.Id, Status = "accepted" };
    }

    public async Task<IReadOnlyList<LocationCreateResponseDto>> CreateBatchAsync(LocationBatchCreateDto request, CancellationToken ct)
    {
        var device = await _deviceRepository.GetByDeviceIdAsync(request.DeviceId, ct)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        var locations = request.Locations
            .Select(l => new Location(
                device.Id, l.Timestamp, l.Latitude, l.Longitude,
                l.Altitude, l.Speed, l.Satellites, l.Hdop, l.BatteryVoltage, l.IsStale))
            .ToList();

        var saved = await _locationRepository.BatchInsertAsync(locations, ct);

        return saved.Select(l => new LocationCreateResponseDto { Id = l.Id, Status = "accepted" }).ToList();
    }

    public async Task<IReadOnlyList<LocationReadDto>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct)
    {
        var locations = await _locationRepository.GetLatestByDeviceAsync(deviceId, ct);

        return locations.Select(l => new LocationReadDto
        {
            Id = l.Id,
            DeviceId = deviceId,
            Timestamp = l.Timestamp,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
            Altitude = l.Altitude,
            Speed = l.Speed,
            Satellites = l.Satellites,
            Hdop = l.Hdop,
            BatteryVoltage = l.BatteryVoltage,
            IsStale = l.IsStale
        }).ToList();
    }
}
```

Write `backend/AssetTracker.Application/Services/DeviceService.cs`:
```csharp
using System.Security.Cryptography;
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Exceptions;
using AssetTracker.Application.Interfaces;

namespace AssetTracker.Application.Services;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;

    public DeviceService(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<DeviceRegisterResponseDto> RegisterAsync(DeviceRegisterRequestDto request, CancellationToken ct)
    {
        var existing = await _deviceRepository.GetByDeviceIdAsync(request.DeviceId, ct);
        if (existing is not null)
            throw new DeviceAlreadyExistsException(request.DeviceId);

        var apiKeyBytes = RandomNumberGenerator.GetBytes(32);
        var apiKey = Convert.ToBase64String(apiKeyBytes);
        var apiKeyHash = SHA256.HashData(apiKeyBytes);

        await _deviceRepository.RegisterAsync(request.DeviceId, apiKeyHash, request.DisplayName, ct);

        return new DeviceRegisterResponseDto
        {
            DeviceId = request.DeviceId,
            ApiKey = apiKey
        };
    }
}
```

- [ ] **Step 4: Register in DI**

Modify `backend/AssetTracker.Application/DependencyInjection.cs` — replace its full contents:
```csharp
using AssetTracker.Application.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IDeviceService, DeviceService>();

        return services;
    }
}
```

- [ ] **Step 5: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~LocationServiceTests|FullyQualifiedName~DeviceServiceTests"`
Expected: PASS (5 tests)

- [ ] **Step 6: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Application backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: LocationService + DeviceService business logic

DeviceService generates the random API key (32 bytes, SHA-256 hashed
for storage) that Task 12's API-key auth handler validates against.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 12: Controllers + Authentication Wiring

This is the integration point: authentication schemes only make sense once there are real endpoints to protect, and endpoints need auth wired to be testable — so this task builds both together rather than splitting them (see "Task Right-Sizing": these two things can't be independently reviewed/tested apart from each other).

**Files:**
- Create: `backend/AssetTracker.Api/Auth/AuthSchemes.cs`
- Create: `backend/AssetTracker.Api/Auth/ApiKeyAuthenticationSchemeOptions.cs`
- Create: `backend/AssetTracker.Api/Auth/ApiKeyAuthenticationHandler.cs`
- Create: `backend/AssetTracker.Api/Middleware/ErrorHandlingMiddleware.cs`
- Create: `backend/AssetTracker.Api/Controllers/AuthController.cs`
- Create: `backend/AssetTracker.Api/Controllers/DevicesController.cs`
- Create: `backend/AssetTracker.Api/Controllers/LocationsController.cs`
- Create: `backend/AssetTracker.Tests/Integration/ApiFactoryFixture.cs`
- Create: `backend/AssetTracker.Tests/Integration/TestAuthHelper.cs`
- Create: `backend/AssetTracker.Tests/Integration/AuthEndpointTests.cs`
- Create: `backend/AssetTracker.Tests/Integration/DevicesEndpointTests.cs`
- Create: `backend/AssetTracker.Tests/Integration/LocationsEndpointTests.cs`
- Modify: `backend/AssetTracker.Api/AssetTracker.Api.csproj` (add JwtBearer, Swashbuckle)
- Modify: `backend/AssetTracker.Api/Program.cs` (final version)

**Interfaces:**
- Consumes: `IAuthService`, `ILocationService`, `IDeviceService` (Task 10/11), `IDeviceRepository.GetByApiKeyHashAsync` (Task 4), all `Dtos` (Task 8)
- Produces: `AuthSchemes.Jwt = "Jwt"`, `AuthSchemes.ApiKey = "ApiKey"` (referenced by every `[Authorize(AuthenticationSchemes = ...)]` attribute below)

- [ ] **Step 1: Add packages**

```bash
cd backend
dotnet add AssetTracker.Api/AssetTracker.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.11
dotnet add AssetTracker.Api/AssetTracker.Api.csproj package Swashbuckle.AspNetCore --version 10.2.3
```

- [ ] **Step 2: Write the full integration test suite (failing first)**

Write `backend/AssetTracker.Tests/Integration/ApiFactoryFixture.cs`:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssetTracker.Tests.Integration;

public class ApiFactoryFixture : WebApplicationFactory<Program>
{
    public string ConnectionString { get; set; } = string.Empty;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ConnectionString,
                ["Jwt:Key"] = "a-test-signing-key-that-is-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "AssetTrackerApi",
                ["Jwt:Audience"] = "AssetTrackerDashboard",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });
    }
}
```

`ConnectionString` must be set before the first `CreateClient()` call on a given instance (the host builds lazily on first use) — every test class below sets it in its constructor before touching the client.

Write `backend/AssetTracker.Tests/Integration/TestAuthHelper.cs`:
```csharp
using System.Net.Http.Json;
using AssetTracker.Application.Dtos;

namespace AssetTracker.Tests.Integration;

public static class TestAuthHelper
{
    public static async Task<string> GetAdminJwtAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "ChangeMe123!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }
}
```

Write `backend/AssetTracker.Tests/Integration/AuthEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Json;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class AuthEndpointTests : IClassFixture<ApiFactoryFixture>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(SqlServerFixture dbFixture, ApiFactoryFixture apiFixture)
    {
        apiFixture.ConnectionString = dbFixture.ConnectionString;
        _client = apiFixture.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "ChangeMe123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

Write `backend/AssetTracker.Tests/Integration/DevicesEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class DevicesEndpointTests : IClassFixture<ApiFactoryFixture>
{
    private readonly HttpClient _client;

    public DevicesEndpointTests(SqlServerFixture dbFixture, ApiFactoryFixture apiFixture)
    {
        apiFixture.ConnectionString = dbFixture.ConnectionString;
        _client = apiFixture.CreateClient();
    }

    [Fact]
    public async Task Register_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new DeviceRegisterRequestDto { DeviceId = "unauthorized-attempt" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidJwt_ReturnsCreatedWithApiKey()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deviceId = $"test-device-{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new DeviceRegisterRequestDto { DeviceId = deviceId, DisplayName = "Test Device" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<DeviceRegisterResponseDto>();
        Assert.Equal(deviceId, body!.DeviceId);
        Assert.False(string.IsNullOrWhiteSpace(body.ApiKey));
    }

    [Fact]
    public async Task Register_WithDuplicateDeviceId_ReturnsConflict()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deviceId = $"test-device-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/devices", new DeviceRegisterRequestDto { DeviceId = deviceId });

        var response = await _client.PostAsJsonAsync("/api/v1/devices", new DeviceRegisterRequestDto { DeviceId = deviceId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

Write `backend/AssetTracker.Tests/Integration/LocationsEndpointTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AssetTracker.Application.Dtos;
using Xunit;

namespace AssetTracker.Tests.Integration;

[Collection("Database")]
public class LocationsEndpointTests : IClassFixture<ApiFactoryFixture>
{
    private readonly HttpClient _client;

    public LocationsEndpointTests(SqlServerFixture dbFixture, ApiFactoryFixture apiFixture)
    {
        apiFixture.ConnectionString = dbFixture.ConnectionString;
        _client = apiFixture.CreateClient();
    }

    private async Task<(string DeviceId, string ApiKey)> RegisterDeviceAsync()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        var deviceId = $"test-device-{Guid.NewGuid():N}";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/devices")
        {
            Content = JsonContent.Create(new DeviceRegisterRequestDto { DeviceId = deviceId })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<DeviceRegisterResponseDto>();

        return (deviceId, body!.ApiKey);
    }

    [Fact]
    public async Task Create_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/locations",
            new LocationCreateDto { DeviceId = "does-not-matter", Timestamp = DateTimeOffset.UtcNow, Latitude = 1, Longitude = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidApiKey_ReturnsCreated()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceId,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = -31.4231,
                Longitude = -62.0834,
                Altitude = 142.1,
                Speed = 0.4,
                Satellites = 9,
                Hdop = 0.8,
                BatteryVoltage = 3.7,
                IsStale = false
            })
        };
        request.Headers.Add("X-API-Key", apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidLatitude_ReturnsValidationError()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceId,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = 999,
                Longitude = 1
            })
        };
        request.Headers.Add("X-API-Key", apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("VALIDATION_ERROR", body!["error"].GetString());
    }

    [Fact]
    public async Task CreateBatch_WithValidApiKey_ReturnsCreatedWithAllItems()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations/batch")
        {
            Content = JsonContent.Create(new LocationBatchCreateDto
            {
                DeviceId = deviceId,
                Locations = new List<LocationCreateDto>
                {
                    new() { DeviceId = deviceId, Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1), Latitude = 1, Longitude = 1 },
                    new() { DeviceId = deviceId, Timestamp = DateTimeOffset.UtcNow, Latitude = 2, Longitude = 2 }
                }
            })
        };
        request.Headers.Add("X-API-Key", apiKey);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LocationCreateResponseDto>>();
        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetLatestByDevice_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/locations/some-device");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLatestByDevice_WithValidJwt_ReturnsLatestLocation()
    {
        var (deviceId, apiKey) = await RegisterDeviceAsync();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceId,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = 5,
                Longitude = 6
            })
        };
        createRequest.Headers.Add("X-API-Key", apiKey);
        (await _client.SendAsync(createRequest)).EnsureSuccessStatusCode();

        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/locations/{deviceId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LocationReadDto>>();
        Assert.Single(body!);
        Assert.Equal(deviceId, body[0].DeviceId);
    }
}
```

- [ ] **Step 3: Run tests, verify they fail to build**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj --filter "FullyQualifiedName~Integration.AuthEndpointTests|FullyQualifiedName~Integration.DevicesEndpointTests|FullyQualifiedName~Integration.LocationsEndpointTests"`
Expected: FAIL to build — `AuthController`/`DevicesController`/`LocationsController` (beyond the existing `HealthController`) don't exist yet, and `/api/v1/devices`, `/api/v1/auth/login` routes 404 even once it compiles against stubs.

- [ ] **Step 4: Implement the auth schemes and handler**

Write `backend/AssetTracker.Api/Auth/AuthSchemes.cs`:
```csharp
namespace AssetTracker.Api.Auth;

public static class AuthSchemes
{
    public const string Jwt = "Jwt";
    public const string ApiKey = "ApiKey";
}
```

Write `backend/AssetTracker.Api/Auth/ApiKeyAuthenticationSchemeOptions.cs`:
```csharp
using Microsoft.AspNetCore.Authentication;

namespace AssetTracker.Api.Auth;

public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}
```

Write `backend/AssetTracker.Api/Auth/ApiKeyAuthenticationHandler.cs`:
```csharp
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
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
}
```

This decode-then-SHA-256 logic must exactly mirror `DeviceService.RegisterAsync` (Task 11): the key handed to the caller is `Convert.ToBase64String(apiKeyBytes)`, so validation decodes it back with `Convert.FromBase64String` before hashing — hashing the base64 *string's* bytes instead of the decoded raw bytes would never match.

- [ ] **Step 5: Implement the error-handling middleware**

Write `backend/AssetTracker.Api/Middleware/ErrorHandlingMiddleware.cs`:
```csharp
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
```

This covers exception-driven errors (device not found/exists, bad credentials). Model-validation errors (400s from `[Required]`/`[Range]` attributes) are handled separately in `Program.cs` (Step 7) via `ApiBehaviorOptions.InvalidModelStateResponseFactory`, since those short-circuit before a controller action — and therefore before this middleware's `try` block would ever see an exception — they're not exceptions at all, MVC's model binding produces them directly.

- [ ] **Step 6: Implement the controllers**

Write `backend/AssetTracker.Api/Controllers/AuthController.cs`:
```csharp
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var response = await _authService.LoginAsync(request, ct);
        return Ok(response);
    }
}
```

Write `backend/AssetTracker.Api/Controllers/DevicesController.cs`:
```csharp
using AssetTracker.Api.Auth;
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Authorize(AuthenticationSchemes = AuthSchemes.Jwt)]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpPost]
    public async Task<ActionResult<DeviceRegisterResponseDto>> Register([FromBody] DeviceRegisterRequestDto request, CancellationToken ct)
    {
        var response = await _deviceService.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
```

Write `backend/AssetTracker.Api/Controllers/LocationsController.cs`:
```csharp
using AssetTracker.Api.Auth;
using AssetTracker.Application.Dtos;
using AssetTracker.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.Api.Controllers;

[ApiController]
[Route("api/v1/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthSchemes.ApiKey)]
    public async Task<ActionResult<LocationCreateResponseDto>> Create([FromBody] LocationCreateDto request, CancellationToken ct)
    {
        var response = await _locationService.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("batch")]
    [Authorize(AuthenticationSchemes = AuthSchemes.ApiKey)]
    public async Task<ActionResult<IReadOnlyList<LocationCreateResponseDto>>> CreateBatch([FromBody] LocationBatchCreateDto request, CancellationToken ct)
    {
        var response = await _locationService.CreateBatchAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{deviceId}")]
    [Authorize(AuthenticationSchemes = AuthSchemes.Jwt)]
    public async Task<ActionResult<IReadOnlyList<LocationReadDto>>> GetLatestByDevice(string deviceId, CancellationToken ct)
    {
        var response = await _locationService.GetLatestByDeviceAsync(deviceId, ct);
        return Ok(response);
    }
}
```

- [ ] **Step 7: Wire everything into `Program.cs` (final version)**

Replace `backend/AssetTracker.Api/Program.cs` in full:
```csharp
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

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key configuration is required.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = AuthSchemes.Jwt;
        options.DefaultChallengeScheme = AuthSchemes.Jwt;
    })
    .AddJwtBearer(AuthSchemes.Jwt, options =>
    {
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
    })
    .AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthSchemes.ApiKey, _ => { });

builder.Services.AddAuthorization();

var app = builder.Build();

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
```

Notes on this version vs. the Task 3/10 versions:
- `app.UseHttpsRedirection()` from the original template is dropped — integration tests hit the in-memory `TestServer` over plain HTTP, and this backend runs behind the Docker/droplet setup without TLS termination at this layer (matches the original spec's local dev posture).
- `AuthSchemes.Jwt`/`AuthSchemes.ApiKey` replace the earlier implicit `JwtBearerDefaults.AuthenticationScheme` ("Bearer") naming so `Program.cs` and every controller's `[Authorize(AuthenticationSchemes = ...)]` attribute reference the exact same constant — no magic strings to typo.
- CORS origin `http://localhost:5173` is the Vite dev server default port from the (unchanged) frontend spec.

- [ ] **Step 8: Run tests, verify they pass**

Run: `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj`
Expected: PASS — full suite, including every earlier task's tests (health, domain, DTOs, security, all repositories, all services, and this task's endpoint tests).

- [ ] **Step 9: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add backend/AssetTracker.Api backend/AssetTracker.Tests
git commit -m "$(cat <<'EOF'
RT: controllers + JWT/API-key auth wiring + error middleware

Wires the full HTTP surface: AuthController, DevicesController,
LocationsController, dual authentication schemes (JWT for the
dashboard admin, API key for devices), standardized error envelope,
CORS, response compression, and Swagger UI.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 13: Docker Compose + Dockerfile + Azure Pipelines CI

**Files:**
- Create: `backend/AssetTracker.Api/Dockerfile`
- Create: `backend/docker-compose.yml`
- Create: `azure-pipelines.yml` (repo root)

**Interfaces:**
- Consumes: nothing from earlier tasks (infrastructure-only, no C# changes)

This is the JD's explicit Azure DevOps requirement made concrete. Scope is build+test only, matching the original spec's "Phase 1: manual deploy" stance — no image push/CD stage yet.

- [ ] **Step 1: Write the Dockerfile**

Write `backend/AssetTracker.Api/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore AssetTracker.Api/AssetTracker.Api.csproj
RUN dotnet publish AssetTracker.Api/AssetTracker.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AssetTracker.Api.dll"]
```

- [ ] **Step 2: Write `docker-compose.yml`**

Write `backend/docker-compose.yml`:
```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${MSSQL_SA_PASSWORD:?set MSSQL_SA_PASSWORD in your environment}"
    ports:
      - "1433:1433"
    volumes:
      - mssql-data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -C -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10

  api:
    build:
      context: .
      dockerfile: AssetTracker.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: "Development"
      ConnectionStrings__Default: "Server=db,1433;Database=AssetTrackerDb;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;"
      Jwt__Key: "${JWT_KEY:?set JWT_KEY in your environment}"
      Jwt__Issuer: "AssetTrackerApi"
      Jwt__Audience: "AssetTrackerDashboard"
    ports:
      - "8080:8080"
    depends_on:
      db:
        condition: service_healthy

volumes:
  mssql-data:
```

`MSSQL_SA_PASSWORD` and `JWT_KEY` are required environment variables with no baked-in default (the `${VAR:?message}` syntax fails fast if unset) — this is the actual "no hardcoded secrets" enforcement point for anything beyond local dev with `appsettings.json`'s placeholder values.

Database migrations aren't applied automatically by this compose file (no migration-runner step) — for local Docker use, run `dotnet ef database update --project AssetTracker.Infrastructure --startup-project AssetTracker.Api` against the compose-started `db` service before hitting the API, or rely on the Testcontainers-backed integration tests (Task 3+) which self-migrate. Automating this is a reasonable follow-up but out of scope here.

- [ ] **Step 3: Write `azure-pipelines.yml`**

Write `azure-pipelines.yml` at the repository root (`/home/rodrigotristany/Documents/asset-tracker-platform/azure-pipelines.yml`):
```yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  solution: 'backend/AssetTracker.sln'

stages:
  - stage: BuildAndTest
    displayName: 'Build and Test Backend'
    jobs:
      - job: Build
        displayName: 'Restore, Build, Test'
        steps:
          - task: UseDotNet@2
            displayName: 'Install .NET 10 SDK'
            inputs:
              packageType: 'sdk'
              version: '10.0.x'

          - script: dotnet restore $(solution)
            displayName: 'dotnet restore'

          - script: dotnet build $(solution) --configuration $(buildConfiguration) --no-restore
            displayName: 'dotnet build'

          - script: dotnet test $(solution) --configuration $(buildConfiguration) --no-build --logger trx --results-directory $(Agent.TempDirectory)/testresults
            displayName: 'dotnet test'

          - task: PublishTestResults@2
            displayName: 'Publish test results'
            condition: succeededOrFailed()
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/*.trx'
              searchFolder: '$(Agent.TempDirectory)/testresults'
```

Integration tests spin up SQL Server via Testcontainers, which needs a working Docker daemon — Microsoft-hosted `ubuntu-latest` Azure Pipelines agents have Docker pre-installed and running, so this works without extra setup. No image push/deploy stage yet, matching the original spec's Phase 1 "manual deploy" scope — that's a natural Phase 2 addition once there's somewhere real to deploy to.

- [ ] **Step 4: Validate the YAML parses**

Run:
```bash
python3 -c "import yaml; yaml.safe_load(open('azure-pipelines.yml')); yaml.safe_load(open('backend/docker-compose.yml')); print('both valid')"
```
Expected: `both valid` (this only checks YAML syntax, not Azure Pipelines schema semantics — there's no local Azure Pipelines validator; real validation happens on first push to a repo with Azure Pipelines connected, which is outside this plan's scope since no Azure DevOps org was set up as part of this work).

- [ ] **Step 5: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add azure-pipelines.yml backend/docker-compose.yml backend/AssetTracker.Api/Dockerfile
git commit -m "$(cat <<'EOF'
RT: Docker Compose + Azure Pipelines CI for the backend

Build+test pipeline (restore/build/test with published trx results),
matching the JD's Azure DevOps requirement. No deploy stage yet — same
"Phase 1: manual deploy" scope as the rest of the backend.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

## Task 14: Rewrite Specs and Agent Rules for C#/.NET

The backend now exists and works — everything below is fixing documentation that still describes the old, never-built Python/FastAPI plan. Per the design doc's §9 "Documentation Impact," this is the last remaining item.

**Files:**
- Modify: `specs/backend/api.md` (full rewrite)
- Modify: `specs/backend/models.md` (full rewrite)
- Modify: `specs/backend/schemas.md` (full rewrite)
- Modify: `specs/spec.md` (§6 full rewrite + scattered FastAPI/PostgreSQL/Pydantic references elsewhere)
- Modify: `specs/diagrams.md` (system architecture + backend layer diagrams)
- Modify: `.clinerules/backend.md` (full rewrite)
- Modify: `.clinerules/architecture.md` (backend layer separation + import allowlist sections)
- Modify: `.clinerules/coding.md` (add a C#-specific naming subsection)
- Modify: `.cline/skills/backend-development/SKILL.md` (full rewrite)

No test cycle here — these are documentation files, so "done" means grep confirms no stale references remain (Step 10).

- [ ] **Step 1: Rewrite `specs/backend/api.md`**

Replace the full file contents:
```markdown
# Backend API

## Endpoints

| Method | Path | Auth | Body | Response | Purpose |
|--------|------|------|------|----------|---------|
| `POST` | `/api/v1/auth/login` | None | `LoginRequestDto` | `LoginResponseDto` | Admin login, issues JWT |
| `POST` | `/api/v1/devices` | JWT | `DeviceRegisterRequestDto` | `DeviceRegisterResponseDto` | Register a device, returns its API key once |
| `POST` | `/api/v1/locations` | `X-API-Key` | `LocationCreateDto` | `LocationCreateResponseDto` | Single upload |
| `POST` | `/api/v1/locations/batch` | `X-API-Key` | `LocationBatchCreateDto` | `LocationCreateResponseDto[]` | Batch upload |
| `GET` | `/api/v1/locations/{deviceId}` | JWT | `-` | `LocationReadDto[]` | Latest location for dashboard (0 or 1 item) |
| `GET` | `/api/v1/health` | None | `-` | `{"status": "ok"}` | Health/connectivity |

## Request/Response Contracts

See `schemas.md` for exact DTOs.

### POST /api/v1/locations

**Request:**
```json
{
    "deviceId": "goat-001",
    "timestamp": "2026-07-29T13:20:00Z",
    "latitude": -31.4231,
    "longitude": -62.0834,
    "altitude": 142.1,
    "speed": 0.4,
    "satellites": 9,
    "hdop": 0.8,
    "batteryVoltage": 3.7,
    "isStale": false
}
```

**Success:** `201 Created`
```json
{"id": 1234, "status": "accepted"}
```

**Error:** `400/401` with standard envelope.

### POST /api/v1/devices

**Request:**
```json
{"deviceId": "goat-001", "displayName": "Goat 001"}
```

**Success:** `201 Created`
```json
{"deviceId": "goat-001", "apiKey": "base64-encoded-32-random-bytes"}
```

The `apiKey` is shown exactly once — only its SHA-256 hash is stored (`devices.api_key_hash`). There is no retrieval endpoint; losing it means re-registering under a new device ID.

**Error:** `401` (missing/invalid JWT), `409` (device ID already registered).

### POST /api/v1/auth/login

**Request:**
```json
{"username": "admin", "password": "..."}
```

**Success:** `200 OK`
```json
{"token": "<jwt>"}
```

**Error:** `401` on wrong username/password (standard envelope, `"error": "INVALID_CREDENTIALS"`).

## Notes
- Devices authenticate via `X-API-Key` header (raw key, base64-encoded 32 random bytes — validated by decoding and re-hashing with SHA-256, then comparing to `devices.api_key_hash`).
- Dashboard admin authenticates via JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`), obtained from `/api/v1/auth/login`.
- Swagger UI served at `/swagger` in the Development environment (via Swashbuckle.AspNetCore) — the ASP.NET Core equivalent of FastAPI's `/docs`.
- Gzip response compression enabled via `Microsoft.AspNetCore.ResponseCompression`.
```

- [ ] **Step 2: Rewrite `specs/backend/models.md`**

Replace the full file contents:
```markdown
# Backend Data Model — SQL Server

## devices

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| `id` | `INT IDENTITY` | NO | auto | Internal PK |
| `device_id` | `VARCHAR(64)` | NO | `-` | Unique business key, e.g. `"goat-001"` |
| `display_name` | `VARCHAR(128)` | YES | `NULL` | |
| `api_key_hash` | `VARBINARY(64)` | NO | `-` | SHA-256 digest of the raw API key (32 bytes) |
| `is_active` | `BIT` | NO | `1` | |
| `created_at` | `DATETIME2` | NO | `SYSUTCDATETIME()` | |

Unique index on `device_id`.

## locations

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| `id` | `BIGINT IDENTITY` | NO | auto | Internal PK |
| `device_fk` | `INT` | NO | `-` | FK → `devices.id` |
| `timestamp` | `DATETIMEOFFSET` | NO | `-` | UTC, from payload |
| `latitude` | `FLOAT` | NO | `-` | WGS84 decimal degrees |
| `longitude` | `FLOAT` | NO | `-` | WGS84 decimal degrees |
| `altitude` | `FLOAT` | YES | `NULL` | Meters above sea level |
| `speed` | `FLOAT` | YES | `NULL` | Meters per second |
| `satellites` | `TINYINT` | YES | `NULL` | Count of satellites in view |
| `hdop` | `FLOAT` | YES | `NULL` | Horizontal dilution of precision |
| `battery_voltage` | `FLOAT` | YES | `NULL` | Volts |
| `is_stale` | `BIT` | NO | `0` | True when this is a fallback/last-known position |
| `created_at` | `DATETIME2` | NO | `SYSUTCDATETIME()` | Inserted by DB |

```sql
CREATE INDEX idx_locations_device_timestamp ON locations (device_fk, timestamp DESC);
```

## admin_users

| Column | Type | Null | Default | Notes |
|--------|------|------|---------|-------|
| `id` | `INT IDENTITY` | NO | auto | Internal PK |
| `username` | `VARCHAR(64)` | NO | `-` | Unique |
| `password_hash` | `VARCHAR(60)` | NO | `-` | BCrypt-encoded hash (self-contained: algorithm + cost + salt + hash) |
| `created_at` | `DATETIME2` | NO | `SYSUTCDATETIME()` | |

Unique index on `username`. No registration endpoint exists for this table — one dev admin user (`admin` / `ChangeMe123!`) is seeded via EF Core migration `HasData`. Rotate or remove this credential before any real deployment.

## Access pattern (hybrid)

| Table | Reads | Writes |
|---|---|---|
| `devices` | EF Core (`GetByDeviceIdAsync`) | Stored procedure (`usp_Device_Register`, `usp_Device_GetByApiKeyHash`) |
| `locations` | Stored procedure (`usp_Location_GetLatestByDevice`) | Stored procedure (`usp_Location_Insert`, `usp_Location_BatchInsert` via table-valued parameter) |
| `admin_users` | EF Core (`GetByUsernameAsync`) | Migration seed only, no runtime writes |

See `../diagrams.md` for the ORM/Dapper/stored-procedure layering.

## Stored Procedures

| Procedure | Purpose |
|---|---|
| `usp_Device_Register` | Insert a device row, return it |
| `usp_Device_GetByApiKeyHash` | Device auth lookup |
| `usp_Location_Insert` | Single location write |
| `usp_Location_BatchInsert` | Batch write via `LocationTableType` table-valued parameter |
| `usp_Location_GetLatestByDevice` | Latest location for the dashboard |
| `usp_Retention_PurgeOldLocations` | Deletes rows older than the retention window (default 30 days), returns count deleted |

## Retention
- 30-day rolling window, enforced by `usp_Retention_PurgeOldLocations`.
- Scheduling mechanism (SQL Server Agent job vs. hosted background service) is an open TBD — see the design doc, §11.
```

- [ ] **Step 3: Rewrite `specs/backend/schemas.md`**

Replace the full file contents:
```markdown
# Backend DTOs — `AssetTracker.Application.Dtos`

Validated with `System.ComponentModel.DataAnnotations`. JSON is camelCase on the wire (ASP.NET Core's default `System.Text.Json` naming policy) — no `[JsonPropertyName]` attributes needed anywhere below.

## LocationCreateDto (Request Body)

```csharp
public class LocationCreateDto
{
    [Required] public string DeviceId { get; set; } = string.Empty;
    [Required] public DateTimeOffset Timestamp { get; set; }
    [Range(-90, 90)] public double Latitude { get; set; }
    [Range(-180, 180)] public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public byte? Satellites { get; set; }
    public double? Hdop { get; set; }
    public double? BatteryVoltage { get; set; }
    public bool IsStale { get; set; }
}
```

## LocationBatchCreateDto (Batch Upload)

```csharp
public class LocationBatchCreateDto
{
    [Required] public string DeviceId { get; set; } = string.Empty;
    [Required, MinLength(1)] public List<LocationCreateDto> Locations { get; set; } = new();
}
```

## LocationCreateResponseDto (Response)

```csharp
public class LocationCreateResponseDto
{
    public long Id { get; set; }
    public string Status { get; set; } = "accepted";
}
```

## LocationReadDto (DB → API)

```csharp
public class LocationReadDto
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public byte? Satellites { get; set; }
    public double? Hdop { get; set; }
    public double? BatteryVoltage { get; set; }
    public bool IsStale { get; set; }
}
```

## DeviceRegisterRequestDto / DeviceRegisterResponseDto

```csharp
public class DeviceRegisterRequestDto
{
    [Required] public string DeviceId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public class DeviceRegisterResponseDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty; // shown once, never stored in plaintext
}
```

## LoginRequestDto / LoginResponseDto

```csharp
public class LoginRequestDto
{
    [Required] public string Username { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
}
```

## Standard Error Envelope

Not a DTO class — built inline by `ErrorHandlingMiddleware` (exception-driven errors) and `ApiBehaviorOptions.InvalidModelStateResponseFactory` (validation errors) in `AssetTracker.Api`:
```json
{"error": "VALIDATION_ERROR", "message": "One or more validation errors occurred.", "details": {"latitude": ["The field Latitude must be between -90 and 90."]}}
```
```

- [ ] **Step 4: Rewrite `specs/spec.md` §6 (Backend Specification)**

In `specs/spec.md`, replace everything from `## 6. Backend Specification` through (but not including) `## 7. Web Dashboard Specification` — i.e. replace the old_string starting at `## 6. Backend Specification` and ending right before `## 7. Web Dashboard Specification`, keeping the `---` separator immediately before `## 7.`:

Old section header block to replace (find `## 6. Backend Specification` and everything up to the `---` that precedes `## 7. Web Dashboard Specification`):
```markdown
## 6. Backend Specification

### 6.1 Technology Stack

| Component | Technology |
|-----------|------------|
| **Runtime** | Python 3.x |
| **Framework** | FastAPI |
| **ORM** | SQLAlchemy |
| **Migrations** | Alembic |
| **Validation** | Pydantic |
| **Database** | PostgreSQL |
| **Deployment** | Docker Compose (local and production) |
| **Host (Production)** | DigitalOcean Droplet (future) |

### 6.2 Hosting Strategy

**Local Development:**
- Docker Compose orchestrates FastAPI + PostgreSQL
- Hot reload enabled for rapid iteration
- Dashboard served by FastAPI static files or separate Vite dev server (proxied)

**Production (DigitalOcean Droplet):**
- Docker Compose preferred for reproducibility and one-command deploys
- Alternative: bare-metal `uvicorn` + PostgreSQL if Droplet memory is constrained (1-2GB)
- Decision deferred until first production deploy attempt

### 6.3 Authentication

| Actor | Mechanism | Notes |
|-------|-----------|-------|
| **Devices** | Static API key in `X-API-Key` header | Simple, proven, fits "prove pipeline" scope |
| **Dashboard Admin** | JWT with session storage | Read-only access; admin role only |

### 6.4 API Endpoints

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| `POST` | `/api/v1/locations` | Single location upload | Device API key |
| `POST` | `/api/v1/locations/batch` | Batch upload (reconnection scenarios) | Device API key |
| `GET` | `/api/v1/locations/{device_id}` | Latest locations for dashboard | JWT session |
| `GET` | `/api/v1/health` | Health check / connectivity verification | None |

### 6.5 Data Schema

```sql
-- locations table
CREATE TABLE locations (
    id SERIAL PRIMARY KEY,
    device_id VARCHAR(64) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    altitude DOUBLE PRECISION,
    speed DOUBLE PRECISION,
    satellites SMALLINT,
    hdop DOUBLE PRECISION,
    battery_voltage DOUBLE PRECISION,
    is_stale BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

CREATE INDEX idx_locations_device_timestamp ON locations (device_id, timestamp DESC);
```

**Retention:** 30-day rolling window. Automated cleanup job (Alembic migration + cron or TTL).

### 6.6 Request/Response Contracts

**POST /api/v1/locations**
```json
{
    "deviceId": "goat-001",
    "timestamp": "2026-07-29T13:20:00Z",
    "latitude": -31.4231,
    "longitude": -62.0834,
    "altitude": 142.1,
    "speed": 0.4,
    "satellites": 9,
    "hdop": 0.8,
    "batteryVoltage": 3.7,
    "isStale": false
}
```

**Response (201 Created)**
```json
{
    "id": 1234,
    "status": "accepted"
}
```

**Errors:**
- `400 Bad Request` — Validation error (missing fields, invalid coordinates)
- `401 Unauthorized` — Missing or invalid API key
- `429 Too Many Requests` — Rate limit exceeded (future; not blocking Phase 1)

### 6.7 Additional Backend Features

- **CORS:** Enabled for local dashboard development
- **OpenAPI:** Auto-generated at `/docs` (Swagger UI) and `/redoc`
- **Compression:** Gzip enabled for responses
- **Logging:** Structured JSON logs with request ID tracing
```

New section 6 content:
```markdown
## 6. Backend Specification

### 6.1 Technology Stack

| Component | Technology |
|-----------|------------|
| **Runtime** | .NET (latest LTS) |
| **Framework** | ASP.NET Core, Controller-based MVC |
| **Architecture** | Clean Architecture — `Domain` → `Application` → `Infrastructure` → `Api` |
| **Data Access** | Hybrid: EF Core (reads/simple CRUD) + Dapper-driven stored procedures (location/device writes, retention) |
| **Validation** | `System.ComponentModel.DataAnnotations` |
| **Database** | SQL Server |
| **Deployment** | Docker Compose (local and production) |
| **Host (Production)** | DigitalOcean Droplet (future) |
| **CI** | Azure Pipelines (build + test) |

This stack (vs. the originally-specced Python/FastAPI/PostgreSQL) was chosen deliberately to demonstrate .NET/C#, SQL Server (including stored procedures and database architecture), and Azure DevOps skills — see `docs/superpowers/specs/2026-08-12-backend-csharp-design.md` for the full rationale.

### 6.2 Hosting Strategy

**Local Development:**
- Docker Compose orchestrates the API + SQL Server (`mcr.microsoft.com/mssql/server:2022-latest`)
- Dashboard served by a separate Vite dev server during development

**Production (DigitalOcean Droplet):**
- Docker Compose preferred for reproducibility and one-command deploys
- **Known risk:** SQL Server needs materially more RAM (~2GB minimum) than the PostgreSQL it replaces; the droplet's memory sizing needs revisiting before a real production deploy (open TBD)

### 6.3 Authentication

| Actor | Mechanism | Notes |
|-------|-----------|-------|
| **Devices** | API key in `X-API-Key` header | Base64-encoded 32 random bytes; validated by re-hashing (SHA-256) and comparing to `devices.api_key_hash` |
| **Dashboard Admin** | JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) | Obtained via `POST /api/v1/auth/login`; one dev-seeded admin user, no self-registration |

### 6.4 API Endpoints

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| `POST` | `/api/v1/auth/login` | Admin login, issues JWT | None |
| `POST` | `/api/v1/devices` | Register a device, returns its API key once | JWT |
| `POST` | `/api/v1/locations` | Single location upload | Device API key |
| `POST` | `/api/v1/locations/batch` | Batch upload (reconnection scenarios) | Device API key |
| `GET` | `/api/v1/locations/{deviceId}` | Latest location for dashboard | JWT |
| `GET` | `/api/v1/health` | Health check / connectivity verification | None |

`/api/v1/devices` and `/api/v1/auth/login` are additions beyond the original Python-era spec — they exist because the expanded, normalized schema (§6.5) requires a real `devices` row (with a hashed API key) before any location can be written.

### 6.5 Data Schema

```sql
CREATE TABLE devices (
    id INT IDENTITY PRIMARY KEY,
    device_id VARCHAR(64) NOT NULL UNIQUE,
    display_name VARCHAR(128) NULL,
    api_key_hash VARBINARY(64) NOT NULL,
    is_active BIT NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE locations (
    id BIGINT IDENTITY PRIMARY KEY,
    device_fk INT NOT NULL FOREIGN KEY REFERENCES devices(id),
    [timestamp] DATETIMEOFFSET NOT NULL,
    latitude FLOAT NOT NULL,
    longitude FLOAT NOT NULL,
    altitude FLOAT NULL,
    speed FLOAT NULL,
    satellites TINYINT NULL,
    hdop FLOAT NULL,
    battery_voltage FLOAT NULL,
    is_stale BIT NOT NULL DEFAULT 0,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE INDEX idx_locations_device_timestamp ON locations (device_fk, [timestamp] DESC);

CREATE TABLE admin_users (
    id INT IDENTITY PRIMARY KEY,
    username VARCHAR(64) NOT NULL UNIQUE,
    password_hash VARCHAR(60) NOT NULL,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
```

**Retention:** 30-day rolling window, enforced by the `usp_Retention_PurgeOldLocations` stored procedure. Scheduling mechanism (SQL Agent job vs. hosted background service) is an open TBD.

See `specs/backend/models.md` for the full table/procedure reference and the EF Core/Dapper access-pattern split.

### 6.6 Request/Response Contracts

**POST /api/v1/locations**
```json
{
    "deviceId": "goat-001",
    "timestamp": "2026-07-29T13:20:00Z",
    "latitude": -31.4231,
    "longitude": -62.0834,
    "altitude": 142.1,
    "speed": 0.4,
    "satellites": 9,
    "hdop": 0.8,
    "batteryVoltage": 3.7,
    "isStale": false
}
```

**Response (201 Created)**
```json
{
    "id": 1234,
    "status": "accepted"
}
```

**Errors:**
- `400 Bad Request` — Validation error (missing fields, invalid coordinates)
- `401 Unauthorized` — Missing or invalid API key / JWT
- `404 Not Found` — Unknown `deviceId` on a location write
- `409 Conflict` — Duplicate `deviceId` on device registration

See `specs/backend/api.md` for the full endpoint reference including the new `/devices` and `/auth/login` contracts.

### 6.7 Additional Backend Features

- **CORS:** Enabled for local dashboard development (`http://localhost:5173`)
- **OpenAPI:** Auto-generated Swagger UI at `/swagger` (Swashbuckle.AspNetCore) in the Development environment
- **Compression:** Gzip enabled for responses (`Microsoft.AspNetCore.ResponseCompression`)
- **Logging:** Structured logs; unhandled exceptions logged with request trace ID via `ErrorHandlingMiddleware`
```

- [ ] **Step 5: Fix scattered FastAPI/PostgreSQL/Pydantic references elsewhere in `specs/spec.md`**

In section `## 2. Architecture Overview`, replace:
```
REST API (FastAPI)
```
with:
```
REST API (ASP.NET Core)
```

In section `## 3. Data Flow`, replace:
```
5. Backend validates, persists to PostgreSQL
```
with:
```
5. Backend validates, persists to SQL Server
```

In section `## 8.1 Location Schema (Shared)`, replace:
```
// Shared TypeScript type (also reflected in Pydantic model)
```
with:
```
// Shared TypeScript type (also reflected in AssetTracker.Application.Dtos.LocationCreateDto)
```

In section `## 9. Technology Stack Summary`, replace the entire `### Backend` subsection:
```markdown
### Backend

- **Language:** Python 3.x
- **Framework:** FastAPI
- **ORM:** SQLAlchemy 2.x
- **Migrations:** Alembic
- **Validation:** Pydantic v2
- **Database:** PostgreSQL
- **Deployment:** Docker Compose
```
with:
```markdown
### Backend

- **Language:** C# / .NET (latest LTS)
- **Framework:** ASP.NET Core (Controller MVC)
- **Architecture:** Clean Architecture (Domain/Application/Infrastructure/Api)
- **Data Access:** EF Core (reads/simple CRUD) + Dapper-driven stored procedures (writes)
- **Database:** SQL Server
- **Deployment:** Docker Compose
- **CI:** Azure Pipelines
```

In section `## 10. Milestones`, Phase 1 Milestone 6, replace:
```
6. **HTTP POST to API** — Send parsed location to local FastAPI server
```
with:
```
6. **HTTP POST to API** — Send parsed location to local ASP.NET Core server
```

Phase 1 Milestone 7, replace:
```
7. **PostgreSQL Persistence** — Backend receives and stores location in database
```
with:
```
7. **SQL Server Persistence** — Backend receives and stores location in database
```

In section `## 12. Documentation Plan`, replace the `Backend Setup` row:
```
| **Backend Setup** | `docs/backend-setup.md` | Docker Compose, database migrations, virtual env |
```
with:
```
| **Backend Setup** | `docs/backend-setup.md` | Docker Compose, EF Core migrations, .NET SDK setup |
```

In section `## 13. CI/CD`, replace:
```markdown
**Phase 1:** Manual builds for both firmware and backend.

**Phase 2 (Planned):**
- **GitHub Actions:**
  - Lint and type-check (frontend and backend)
  - Backend unit + integration tests on every PR
  - Firmware build verification (compile check without flash)
  - Docker image build and push on merge to main
- **Artifacts:**
  - Firmware binaries (auto-generated)
  - Backend Docker images
  - Dashboard static build
```
with:
```markdown
**Backend:** Azure Pipelines (`azure-pipelines.yml`, repo root) runs restore/build/test on every push to `main`, including integration tests against a Testcontainers-provisioned SQL Server. No deploy stage yet.

**Phase 2 (Planned):**
- Extend the Azure Pipelines definition with lint/type-check for firmware and frontend
- Firmware build verification (compile check without flash)
- Docker image build and push on merge to main
- **Artifacts:**
  - Firmware binaries (auto-generated)
  - Backend Docker images
  - Dashboard static build
```

In section `## 14. Repository Structure`, replace the `backend/` subtree:
```
├── backend/               # FastAPI application
│   ├── app/
│   ├── alembic/
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── pyproject.toml / requirements.txt
```
with:
```
├── backend/               # ASP.NET Core Clean Architecture solution
│   ├── AssetTracker.sln
│   ├── AssetTracker.Domain/
│   ├── AssetTracker.Application/
│   ├── AssetTracker.Infrastructure/
│   ├── AssetTracker.Api/
│   │   └── Dockerfile
│   ├── AssetTracker.Tests/
│   └── docker-compose.yml
```

- [ ] **Step 6: Update `specs/diagrams.md` — System Architecture diagram**

In diagram `## 1. System Architecture`, inside the `subgraph Backend [Backend Layer]` block, replace:
```
        API[FastAPI REST API]
        DB[(PostgreSQL 30d retention)]
```
with:
```
        API[ASP.NET Core REST API]
        DB[(SQL Server 30d retention)]
```

- [ ] **Step 7: Update `specs/diagrams.md` — Backend Layer Architecture diagram**

Replace the entire `## 4. Backend Layer Architecture` diagram block:
```markdown
## 4. Backend Layer Architecture

```mermaid
flowchart TD
    subgraph Router [Routes Layer app/routers]
        R1[locations.py]
        R2[health.py]
    end

    subgraph Service [Service Layer app/services]
        S1[location_service.py]
        S2[auth_service.py]
    end

    subgraph Repo [Repository Layer app/repositories]
        REPO[location_repository.py]
    end

    subgraph DB [(PostgreSQL)]
        T[(locations table)]
    end

    subgraph DTO [Pydantic DTOs app/schemas]
        D1[LocationCreate]
        D2[LocationRead]
        D3[ErrorResponse]
    end

    R1 --> S1
    R2 --> S1
    S1 --> REPO
    REPO --> T
    R1 --> D1
    R1 --> D2
    S1 --> D2
    REPO --> D2

    style Router fill:#bbf,stroke:#333,stroke-width:2px
    style Service fill:#bfb,stroke:#333,stroke-width:2px
    style Repo fill:#ffb,stroke:#333,stroke-width:2px
    style DB fill:#f99,stroke:#333,stroke-width:2px
    style DTO fill:#9f9,stroke:#333,stroke-width:1px,stroke-dasharray: 5 5
```
```
with:
```markdown
## 4. Backend Layer Architecture

```mermaid
flowchart TD
    subgraph Api [AssetTracker.Api]
        C1[LocationsController]
        C2[DevicesController]
        C3[AuthController]
    end

    subgraph App [AssetTracker.Application]
        S1[LocationService]
        S2[DeviceService]
        S3[AuthService]
        D1[Dtos]
    end

    subgraph Infra [AssetTracker.Infrastructure]
        REPO1[LocationRepository - Dapper/SPs]
        REPO2[DeviceRepository - Dapper/SPs + EF read]
        REPO3[AdminUserRepository - EF Core]
        CTX[AssetTrackerDbContext]
    end

    subgraph DB [(SQL Server)]
        T1[(locations)]
        T2[(devices)]
        T3[(admin_users)]
        SP[Stored Procedures]
    end

    C1 --> S1
    C2 --> S2
    C3 --> S3
    S1 --> REPO1
    S2 --> REPO2
    S3 --> REPO3
    REPO1 --> SP
    REPO2 --> SP
    REPO2 --> CTX
    REPO3 --> CTX
    SP --> T1
    SP --> T2
    CTX --> T2
    CTX --> T3
    C1 --> D1
    C2 --> D1
    C3 --> D1

    style Api fill:#bbf,stroke:#333,stroke-width:2px
    style App fill:#bfb,stroke:#333,stroke-width:2px
    style Infra fill:#ffb,stroke:#333,stroke-width:2px
    style DB fill:#f99,stroke:#333,stroke-width:2px
```
```

- [ ] **Step 8: Rewrite `.clinerules/backend.md`**

Replace the full file contents:
```markdown
---
paths:
  - "backend/**"
---

# Backend Guidelines

## Architecture
- Clean Architecture, four projects: `AssetTracker.Domain` → `AssetTracker.Application` → `AssetTracker.Infrastructure` → `AssetTracker.Api`. Dependencies point inward only — `Domain` has zero project references, `Api` may reference `Application` and `Infrastructure`, never the other way around.
- Controllers call `Application` services; services call `Application` repository interfaces; `Infrastructure` implements those interfaces. Controllers must never reference `Infrastructure` types directly.
- Organize controllers by resource (`LocationsController`, `DevicesController`, `AuthController`), all under `api/v1/`.

## Data Access (Hybrid)
- Reads and simple CRUD (e.g. `AdminUser`) go through EF Core (`AssetTrackerDbContext`).
- Location and device write/lookup paths, and retention purge, go through hand-written SQL Server **stored procedures** called via Dapper (`Microsoft.Data.SqlClient` + `Dapper`). Don't add new stored procedures for operations that are simple EF Core CRUD — the split exists to demonstrate real stored-procedure skill on the operations that warrant it, not to route everything through raw SQL.
- Stored procedure files live in `AssetTracker.Infrastructure/Data/StoredProcedures/*.sql`, applied via EF Core migrations (`migrationBuilder.Sql(...)`), and are also the single source of truth for the SQL — don't let the C# and the `.sql` file drift.
- Stored procedure naming: `usp_<Entity>_<Action>` (e.g. `usp_Location_Insert`). Output/select columns are aliased to PascalCase to match Dapper's default column-to-property mapping.
- Never expose EF Core entities or Dapper row-mapping classes directly in API responses; use `Application.Dtos`.

## Database Conventions
- **Table names:** plural `snake_case` (e.g., `locations`).
- **Column names:** `snake_case`.
- **Migrations:** generated via `dotnet ef migrations add <Name> --project AssetTracker.Infrastructure --startup-project AssetTracker.Api`.
- **Queries:** always parameterized (Dapper's anonymous-object/`DynamicParameters` binding, or EF Core LINQ) — never raw string interpolation into SQL.

## Authentication
- Devices authenticate via `X-API-Key` header: base64-encoded 32 random bytes, validated by decoding + SHA-256 re-hashing + comparing to `devices.api_key_hash`. Never store a device's raw API key — only its hash.
- Dashboard admin authenticates via JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`), issued from `POST /api/v1/auth/login`. Passwords hashed with BCrypt (`BCrypt.Net-Next`), never a raw/reversible hash.
- Two named authentication schemes, both referenced via the `AuthSchemes` constants class — never a raw string literal in an `[Authorize(AuthenticationSchemes = ...)]` attribute.

## Error Responses
- Standardized envelope on every 4xx/5xx:
  ```json
  {
    "error": "VALIDATION_ERROR",
    "message": "Human-readable description",
    "details": {}
  }
  ```
- Exception-driven errors (not-found, conflict, invalid credentials) go through `ErrorHandlingMiddleware`, mapped via custom `Application.Exceptions` types.
- Model validation errors (from `DataAnnotations` attributes) go through `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Program.cs`, not the middleware — they never reach a controller action to throw.

## Configuration
- `appsettings.json` holds obviously-fake local-dev defaults only — never a real secret.
- Production values come from environment variables (`ConnectionStrings__Default`, `Jwt__Key`, etc.) — the .NET config system's double-underscore convention for nested keys.
- Never commit a real connection string, JWT signing key, or database password.

## Observability
- Unhandled exceptions logged via `ErrorHandlingMiddleware` with the request's `TraceIdentifier`.
- CORS enabled for local dashboard development (`http://localhost:5173`).
- Swagger UI at `/swagger` in the Development environment (Swashbuckle.AspNetCore).
- Gzip response compression enabled (`Microsoft.AspNetCore.ResponseCompression`).
```

- [ ] **Step 9: Update `.clinerules/architecture.md`**

Replace the `## Backend Layer Separation` section:
```markdown
## Backend Layer Separation
- Strict 3-layer architecture:
  1. **Routes** (`app/routers/`): Handle HTTP, call services.
  2. **Services** (`app/services/`): Business logic, orchestration.
  3. **Repositories** (`app/repositories/`): Database access only.
- Pydantic DTOs must **never** import SQLAlchemy models.
- Routers must go through the service layer; no direct repository access from routes.
```
with:
```markdown
## Backend Layer Separation
- Clean Architecture, four projects, dependencies point inward only:
  1. **Api** (`AssetTracker.Api`): Controllers handle HTTP, call `Application` services.
  2. **Application** (`AssetTracker.Application`): Services (business logic), DTOs, repository interfaces.
  3. **Infrastructure** (`AssetTracker.Infrastructure`): EF Core `DbContext`, repository implementations (Dapper/stored-procedure or EF Core), security (password hashing, JWT generation).
  4. **Domain** (`AssetTracker.Domain`): Entities only, zero project references.
- `Application.Dtos` must **never** reference `Domain` entities directly in their public shape, and must never import EF Core or Dapper types.
- Controllers must go through the service layer; no direct repository or `DbContext` access from a controller.
```

And in section `## Cross-Layer Independence`, replace:
```
- Backend Pydantic schemas for `Location` mirror the firmware struct but are **independent types**; no shared code between firmware and backend.
```
with:
```
- Backend `LocationCreateDto`/`LocationReadDto` mirror the firmware struct but are **independent types**; no shared code between firmware and backend.
```

And in section `## Import / Dependency Allowlists`, replace the `### Backend` subsection:
```markdown
### Backend
- `app/routers/locations.py` may import from `app/services/location_service.py`.
- `app/routers/locations.py` must **not** import from `app/repositories/`.
- `app/schemas/` must not import from `app/models/`.
- **No circular dependencies** between any modules.
```
with:
```markdown
### Backend
- `AssetTracker.Api` may reference `AssetTracker.Application` and `AssetTracker.Infrastructure` (for DI registration only — controllers themselves inject `Application` interfaces, never `Infrastructure` concrete types).
- `AssetTracker.Application` may reference `AssetTracker.Domain` only.
- `AssetTracker.Infrastructure` may reference `AssetTracker.Application` and `AssetTracker.Domain`.
- `AssetTracker.Domain` has zero project references.
- **No circular project references** — enforced structurally by the four-project split; the .NET build itself rejects a cycle.
```

And in section `## Configuration Isolation`, replace:
```
- Backend configuration is loaded exclusively from `.env` files via `pydantic-settings`.
```
with:
```
- Backend configuration is loaded from `appsettings.json` (fake local-dev defaults only) and environment variables (`ConnectionStrings__Default`, `Jwt__Key`, etc. — real values in any non-local environment).
```

- [ ] **Step 10: Add a C# naming subsection to `.clinerules/coding.md`**

In `.clinerules/coding.md`, under `## Naming Conventions`, replace:
```markdown
## Naming Conventions
- **Functions / variables / files:** `snake_case`
- **Classes / structs / types:** `PascalCase`
- **Constants / macros:** `UPPER_SNAKE_CASE`
- **JSON / API payloads:** `camelCase` (matches spec schema)
- **Database columns / tables:** `snake_case` (per backend.md)
```
with:
```markdown
## Naming Conventions
- **Functions / variables / files (C++, Python where applicable):** `snake_case`
- **Classes / structs / types (all languages):** `PascalCase`
- **Constants / macros:** `UPPER_SNAKE_CASE`
- **JSON / API payloads:** `camelCase` (matches spec schema)
- **Database columns / tables:** `snake_case` (per backend.md)

### C# (backend) — supersedes the blanket rule above
- **Types, public members, methods:** `PascalCase` (e.g., `LocationService`, `GetLatestByDeviceAsync`).
- **Local variables, method parameters:** `camelCase`.
- **Interfaces:** `I` prefix + `PascalCase` (e.g., `ILocationRepository`).
- **Private fields:** `_camelCase` (e.g., `_connectionString`).
- **Async methods:** `Async` suffix (e.g., `RegisterAsync`).
- **File names:** match the type they contain, `PascalCase.cs` (e.g., `LocationRepository.cs`).
```

- [ ] **Step 11: Rewrite `.cline/skills/backend-development/SKILL.md`**

Replace the full file contents:
```markdown
---
name: backend-development
description: ASP.NET Core + EF Core + Dapper/stored-procedure backend development with strict Clean Architecture layering
---

triggers:
  paths:
    - "backend/**"
  keywords:
    - "aspnet"
    - "asp.net"
    - "ef core"
    - "entity framework"
    - "dapper"
    - "stored procedure"
    - "sql server"
    - "api"
    - "endpoint"

tool_restrictions:
  allowed:
    - read_files
    - write_files
    - run_commands
    - search_codebase
  disallowed:
    - fetch_web_content

workflow_order:
  - Domain entity (if new)
  - Dto
  - Repository interface (Application) + implementation (Infrastructure)
  - Service (Application)
  - Controller (Api)
  - Tests

## When to use

Use this skill whenever editing or creating files under `backend/`, including ASP.NET Core controllers, DTOs, EF Core entities/migrations, Dapper-based repositories, stored procedures, and Application services.

## Instructions

1. Follow Clean Architecture strictly: `Api` (controllers) → `Application` (services, DTOs, repository interfaces) → `Infrastructure` (EF Core, Dapper, security) → `Domain` (entities, zero dependencies). Dependencies point inward only.
2. Define a DTO in `Application/Dtos` for every API contract. Never expose an EF Core entity or a Dapper row-mapping class directly in a controller response.
3. Data access is hybrid: EF Core for reads/simple CRUD; hand-written stored procedures (called via Dapper) for location/device write paths and retention purge. Don't add a stored procedure for something that's simple EF Core CRUD.
4. Table names must be plural `snake_case` (e.g., `locations`). Column names must be `snake_case`. Stored procedures: `usp_<Entity>_<Action>`, with output columns aliased to PascalCase for clean Dapper mapping.
5. All SQL is parameterized — Dapper anonymous-object/`DynamicParameters` binding or EF Core LINQ. Never interpolate raw strings into a query or a stored procedure call.
6. Device API keys: base64-encoded 32 random bytes, only the SHA-256 hash is ever stored, validated in `ApiKeyAuthenticationHandler` via the `X-API-Key` header.
7. Dashboard admin auth: JWT (`Microsoft.AspNetCore.Authentication.JwtBearer`) issued from `POST /api/v1/auth/login`, passwords hashed with `BCrypt.Net-Next`. Both authentication schemes are referenced via the `AuthSchemes` constants class, never a raw string literal.
8. Return the standardized error envelope on failures: `{"error": "VALIDATION_ERROR", "message": "...", "details": {}}`. Exception-driven errors go through `ErrorHandlingMiddleware`; DataAnnotations validation errors go through `ApiBehaviorOptions.InvalidModelStateResponseFactory` in `Program.cs`.
9. Enable CORS for local dashboard development. Serve Swagger UI at `/swagger` (Development only). Enable Gzip response compression.
10. Only generate an EF Core migration when explicitly asked, via `dotnet ef migrations add <Name> --project AssetTracker.Infrastructure --startup-project AssetTracker.Api`. Stored-procedure-carrying migrations need their `Up`/`Down` filled in by hand (see `AssetTracker.Infrastructure/Data/StoredProcedures/*.sql`) — EF's auto-diff only handles schema, not raw SQL objects.
11. After backend changes, run `dotnet test AssetTracker.Tests/AssetTracker.Tests.csproj` (from `backend/`) to validate behavior before finishing the task. Integration tests need Docker running (Testcontainers spins up a real SQL Server — never substitute an in-memory/SQLite fake).
```

- [ ] **Step 12: Verify no stale references remain**

Run:
```bash
grep -rniE "fastapi|sqlalchemy|alembic|pydantic|postgresql|postgres\b" specs/ .clinerules/ .cline/skills/backend-development/ 2>/dev/null
```
Expected: no output (empty grep result). If anything matches, it's a leftover reference this task missed — fix it before committing.

- [ ] **Step 13: Commit**

```bash
cd /home/rodrigotristany/Documents/asset-tracker-platform
git add specs/ .clinerules/ .cline/skills/backend-development/
git commit -m "$(cat <<'EOF'
RT: rewrite backend specs and agent rules for C#/.NET

Replaces every Python/FastAPI/SQLAlchemy/PostgreSQL reference in
specs/backend/*, specs/spec.md §6, specs/diagrams.md, .clinerules/, and
the backend-development skill with the ASP.NET Core/SQL Server/Clean
Architecture stack actually implemented in Tasks 1-13. Closes out the
design doc's §9 documentation-impact checklist.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---
