# Devices Endpoint Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement `GET /api/v1/devices` (latest location per device, for the dashboard list) in the real C# backend, and fix `GET /api/v1/locations/{deviceId}` to return a single object with `404` instead of a 0-or-1-item array — bringing the already-built backend in line with the specs finalized in `specs/backend/api.md`, `specs/spec.md` §6.4, and `docs/superpowers/specs/2026-08-12-backend-csharp-design.md`.

**Architecture:** Follows the existing Clean Architecture layering already used throughout `backend/` (Controller → `Application` service → `Application` repository interface → `Infrastructure` Dapper/stored-procedure implementation). `GET /api/v1/locations/{deviceId}`'s fix is a same-shape refactor of the existing vertical slice (no new stored procedure — the existing `usp_Location_GetLatestByDevice` already returns 0-or-1 rows; only the C# layers change). `GET /api/v1/devices` is a new vertical slice: a new stored procedure (`usp_Location_GetLatestForAllDevices`, added via a new EF Core migration — the existing `usp_Location_GetLatestByDevice.sql` migration has already shipped and must never be edited in place, per `.clinerules/backend.md`'s migration-immutability rule), a new repository/service method, and a new `DevicesController` GET action.

**Tech Stack:** ASP.NET Core (Controller MVC), Dapper + `Microsoft.Data.SqlClient` for stored-procedure calls, EF Core migrations (SQL-only, no model changes), xUnit + Moq for unit tests, xUnit + Testcontainers (`mcr.microsoft.com/mssql/server:2022-latest`) + `WebApplicationFactory` for integration tests.

## Global Constraints

- Never edit an already-shipped migration's `.sql` file in `AssetTracker.Infrastructure/Data/StoredProcedures/` (`.clinerules/backend.md`) — add a new stored procedure / new migration instead.
- Stored procedure naming: `usp_<Entity>_<Action>`, output columns aliased to PascalCase (`.clinerules/backend.md`).
- Never expose EF Core entities or Dapper row-mapping classes directly in API responses — map through `Application.Dtos` (`.clinerules/backend.md`).
- Controllers must never reference `Infrastructure` types directly; only `Application` services and DTOs (`.clinerules/backend.md`).
- All 4xx/5xx responses use the standard error envelope (`{"error", "message", "details"}`), produced via `Application.Exceptions` + `ErrorHandlingMiddleware`, never written ad hoc in a controller (`.clinerules/backend.md`).
- Queries always parameterized — never raw string interpolation into SQL (`.clinerules/backend.md`).
- No Docker/Testcontainers is available in this working environment. Integration tests (anything under `AssetTracker.Tests/Integration/`) cannot be executed here — write them per TDD as specified, build them successfully, but do not attempt to run them; note in each task's report that they are unexecuted pending a Docker-capable environment. Unit tests (`AssetTracker.Tests/Unit/`) have no such dependency and must actually pass.
- Run `dotnet build` (from `backend/`) after every production-code change to confirm the whole solution still compiles — a build failure blocks the task regardless of which layer's test would have caught it.

---

### Task 1: Fix `GET /api/v1/locations/{deviceId}` to return a single object with 404

**Files:**
- Modify: `backend/AssetTracker.Application/Interfaces/ILocationRepository.cs`
- Modify: `backend/AssetTracker.Infrastructure/Repositories/LocationRepository.cs:87-98`
- Create: `backend/AssetTracker.Application/Exceptions/LocationNotFoundException.cs`
- Modify: `backend/AssetTracker.Api/Middleware/ErrorHandlingMiddleware.cs:43-50`
- Modify: `backend/AssetTracker.Application/Services/ILocationService.cs`
- Modify: `backend/AssetTracker.Application/Services/LocationService.cs:50-68`
- Modify: `backend/AssetTracker.Api/Controllers/LocationsController.cs:53-59`
- Test: `backend/AssetTracker.Tests/Unit/Services/LocationServiceTests.cs:52-64`
- Test: `backend/AssetTracker.Tests/Unit/Middleware/ErrorHandlingMiddlewareTests.cs:44-51`
- Test: `backend/AssetTracker.Tests/Integration/LocationRepositoryTests.cs:103-126`
- Test: `backend/AssetTracker.Tests/Integration/LocationsEndpointTests.cs:303-331`

**Interfaces:**
- Consumes: nothing new — this is a same-shape refactor of an existing vertical slice.
- Produces: `ILocationRepository.GetLatestByDeviceAsync(string deviceId, CancellationToken ct) : Task<Location?>` (was `Task<IReadOnlyList<Location>>`), `ILocationService.GetLatestByDeviceAsync(string deviceId, CancellationToken ct) : Task<LocationReadDto>` (was `Task<IReadOnlyList<LocationReadDto>>`, now throws `LocationNotFoundException` instead of returning an empty list), `LocationNotFoundException` (new, mapped by `ErrorHandlingMiddleware` to `404 LOCATION_NOT_FOUND`). Task 2's `DevicesController` does not depend on any of these — it only depends on new members added in Task 2.

- [ ] **Step 1: Update the four existing tests to express the new single-object/404 contract (RED)**

Replace the existing `GetLatestByDeviceAsync_ReturnsMappedDtos` test in `backend/AssetTracker.Tests/Unit/Services/LocationServiceTests.cs`:

old_string:
```
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
```

new_string:
```
    [Fact]
    public async Task GetLatestByDeviceAsync_ReturnsMappedDto()
    {
        var location = Location.Reconstitute(1, 5, DateTimeOffset.UtcNow, 10, 20, null, null, null, null, null, true, DateTime.UtcNow);
        _locationRepository.Setup(r => r.GetLatestByDeviceAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        var result = await _sut.GetLatestByDeviceAsync("goat-001", CancellationToken.None);

        Assert.Equal("goat-001", result.DeviceId);
        Assert.True(result.IsStale);
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_WithNoRecordedLocation_ThrowsLocationNotFoundException()
    {
        _locationRepository.Setup(r => r.GetLatestByDeviceAsync("goat-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);

        await Assert.ThrowsAsync<LocationNotFoundException>(() => _sut.GetLatestByDeviceAsync("goat-001", CancellationToken.None));
    }
```

Add a new test case to `backend/AssetTracker.Tests/Unit/Middleware/ErrorHandlingMiddlewareTests.cs`:

old_string:
```
    [Fact]
    public async Task InvokeAsync_WithDeviceNotFoundException_Returns404WithDeviceNotFoundError()
    {
        var (statusCode, body) = await InvokeAsync(new DeviceNotFoundException("missing-device"));

        Assert.Equal((int)HttpStatusCode.NotFound, statusCode);
        Assert.Equal("DEVICE_NOT_FOUND", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithDeviceOwnershipMismatchException_Returns403WithForbiddenError()
```

new_string:
```
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
```

Replace the two repository integration tests in `backend/AssetTracker.Tests/Integration/LocationRepositoryTests.cs`:

old_string:
```
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
```

new_string:
```
    [Fact]
    public async Task GetLatestByDeviceAsync_ReturnsOnlyMostRecentLocation()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        await repository.InsertAsync(new Location(device.Id, DateTimeOffset.UtcNow.AddMinutes(-5), 1, 1, null, null, null, null, null, false), CancellationToken.None);
        var newest = await repository.InsertAsync(new Location(device.Id, DateTimeOffset.UtcNow, 9, 9, null, null, null, null, null, false), CancellationToken.None);

        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newest.Id, result!.Id);
    }

    [Fact]
    public async Task GetLatestByDeviceAsync_WithNoLocations_ReturnsNull()
    {
        var device = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);

        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.Null(result);
    }
```

Replace the endpoint integration test in `backend/AssetTracker.Tests/Integration/LocationsEndpointTests.cs` (the final test in the file):

old_string:
```
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
        var single = Assert.Single(body!);
        Assert.Equal(deviceId, single.DeviceId);
    }
}
```

new_string:
```
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
        var body = await response.Content.ReadFromJsonAsync<LocationReadDto>();
        Assert.Equal(deviceId, body!.DeviceId);
    }

    [Fact]
    public async Task GetLatestByDevice_WithNoRecordedLocation_ReturnsNotFound()
    {
        var (deviceId, _) = await RegisterDeviceAsync();
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/locations/{deviceId}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("LOCATION_NOT_FOUND", body!["error"].GetString());
    }
}
```

- [ ] **Step 2: Run the build to confirm RED**

Run (from `backend/`): `dotnet build AssetTracker.slnx`
Expected: FAIL — `AssetTracker.Tests` fails to compile (`LocationNotFoundException` does not exist yet; `GetLatestByDeviceAsync` still returns `IReadOnlyList<Location>`/`IReadOnlyList<LocationReadDto>`, so `.ReturnsAsync(location)` and `result.DeviceId` don't type-check against the old signatures).

- [ ] **Step 3: Create `LocationNotFoundException`**

Create `backend/AssetTracker.Application/Exceptions/LocationNotFoundException.cs`:
```csharp
namespace AssetTracker.Application.Exceptions;

public class LocationNotFoundException : Exception
{
    public LocationNotFoundException(string deviceId) : base($"No location has been recorded for device '{deviceId}'.") { }
}
```

- [ ] **Step 4: Map it in `ErrorHandlingMiddleware`**

old_string:
```
    private static (HttpStatusCode StatusCode, string Error) MapException(Exception exception) => exception switch
    {
        DeviceNotFoundException => (HttpStatusCode.NotFound, "DEVICE_NOT_FOUND"),
        DeviceAlreadyExistsException => (HttpStatusCode.Conflict, "DEVICE_ALREADY_EXISTS"),
        InvalidCredentialsException => (HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS"),
        DeviceOwnershipMismatchException => (HttpStatusCode.Forbidden, "FORBIDDEN"),
        _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
    };
```

new_string:
```
    private static (HttpStatusCode StatusCode, string Error) MapException(Exception exception) => exception switch
    {
        DeviceNotFoundException => (HttpStatusCode.NotFound, "DEVICE_NOT_FOUND"),
        LocationNotFoundException => (HttpStatusCode.NotFound, "LOCATION_NOT_FOUND"),
        DeviceAlreadyExistsException => (HttpStatusCode.Conflict, "DEVICE_ALREADY_EXISTS"),
        InvalidCredentialsException => (HttpStatusCode.Unauthorized, "INVALID_CREDENTIALS"),
        DeviceOwnershipMismatchException => (HttpStatusCode.Forbidden, "FORBIDDEN"),
        _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR")
    };
```

- [ ] **Step 5: Change `ILocationRepository.GetLatestByDeviceAsync` to return a nullable single `Location`**

old_string:
```
    Task<IReadOnlyList<Location>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
```

new_string:
```
    Task<Location?> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
```

- [ ] **Step 6: Update `LocationRepository.GetLatestByDeviceAsync` to match**

old_string:
```
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
```

new_string:
```
    public async Task<Location?> GetLatestByDeviceAsync(string deviceId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<LocationRow>(
            new CommandDefinition(
                "usp_Location_GetLatestByDevice",
                new { DeviceId = deviceId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return row?.ToEntity();
    }
```

- [ ] **Step 7: Change `ILocationService.GetLatestByDeviceAsync` to return a single `LocationReadDto`**

old_string:
```
    Task<IReadOnlyList<LocationReadDto>> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
```

new_string:
```
    Task<LocationReadDto> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
```

- [ ] **Step 8: Update `LocationService.GetLatestByDeviceAsync` to throw `LocationNotFoundException` on a miss**

old_string:
```
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
```

new_string:
```
    public async Task<LocationReadDto> GetLatestByDeviceAsync(string deviceId, CancellationToken ct)
    {
        var location = await _locationRepository.GetLatestByDeviceAsync(deviceId, ct)
            ?? throw new LocationNotFoundException(deviceId);

        return new LocationReadDto
        {
            Id = location.Id,
            DeviceId = deviceId,
            Timestamp = location.Timestamp,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Altitude = location.Altitude,
            Speed = location.Speed,
            Satellites = location.Satellites,
            Hdop = location.Hdop,
            BatteryVoltage = location.BatteryVoltage,
            IsStale = location.IsStale
        };
    }
```

- [ ] **Step 9: Update `LocationsController.GetLatestByDevice`'s return type**

old_string:
```
    [HttpGet("{deviceId}")]
    [Authorize(AuthenticationSchemes = AuthSchemes.Jwt)]
    public async Task<ActionResult<IReadOnlyList<LocationReadDto>>> GetLatestByDevice(string deviceId, CancellationToken ct)
    {
        var response = await _locationService.GetLatestByDeviceAsync(deviceId, ct);
        return Ok(response);
    }
```

new_string:
```
    [HttpGet("{deviceId}")]
    [Authorize(AuthenticationSchemes = AuthSchemes.Jwt)]
    public async Task<ActionResult<LocationReadDto>> GetLatestByDevice(string deviceId, CancellationToken ct)
    {
        var response = await _locationService.GetLatestByDeviceAsync(deviceId, ct);
        return Ok(response);
    }
```

- [ ] **Step 10: Run the build and unit tests to confirm GREEN**

Run (from `backend/`): `dotnet build AssetTracker.slnx`
Expected: PASS — whole solution compiles.

Run (from `backend/`): `dotnet test AssetTracker.Tests --filter "FullyQualifiedName~.Unit." --no-build`
Expected: PASS — all unit tests green, including the two new/changed `LocationServiceTests` cases and the new `ErrorHandlingMiddlewareTests` case.

Per the Global Constraints, do not attempt to run `AssetTracker.Tests/Integration/` (no Docker in this environment) — confirm only that the solution builds, which proves the integration test files themselves compile against the new signatures.

- [ ] **Step 11: Commit**

```bash
cd backend
git add AssetTracker.Application/Interfaces/ILocationRepository.cs \
        AssetTracker.Infrastructure/Repositories/LocationRepository.cs \
        AssetTracker.Application/Exceptions/LocationNotFoundException.cs \
        AssetTracker.Api/Middleware/ErrorHandlingMiddleware.cs \
        AssetTracker.Application/Services/ILocationService.cs \
        AssetTracker.Application/Services/LocationService.cs \
        AssetTracker.Api/Controllers/LocationsController.cs \
        AssetTracker.Tests/Unit/Services/LocationServiceTests.cs \
        AssetTracker.Tests/Unit/Middleware/ErrorHandlingMiddlewareTests.cs \
        AssetTracker.Tests/Integration/LocationRepositoryTests.cs \
        AssetTracker.Tests/Integration/LocationsEndpointTests.cs
git commit -m "RT: return single LocationReadDto with 404 from GET /api/v1/locations/{deviceId}"
```

---

### Task 2: Add `GET /api/v1/devices` (latest location per device)

**Files:**
- Create: `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_GetLatestForAllDevices.sql`
- Create: `backend/AssetTracker.Infrastructure/Data/Migrations/<timestamp>_AddLocationGetLatestForAllDevicesProcedure.cs` (scaffolded via CLI, then hand-edited — see Step 2)
- Modify: `backend/AssetTracker.Application/Interfaces/ILocationRepository.cs`
- Modify: `backend/AssetTracker.Infrastructure/Repositories/LocationRepository.cs`
- Modify: `backend/AssetTracker.Application/Services/ILocationService.cs`
- Modify: `backend/AssetTracker.Application/Services/LocationService.cs`
- Modify: `backend/AssetTracker.Api/Controllers/DevicesController.cs`
- Test: `backend/AssetTracker.Tests/Unit/Services/LocationServiceTests.cs`
- Test: `backend/AssetTracker.Tests/Integration/LocationRepositoryTests.cs`
- Test: `backend/AssetTracker.Tests/Integration/DevicesEndpointTests.cs`

**Interfaces:**
- Consumes: `LocationReadDto` (existing, from Task 1's file — unchanged shape), `Location.Reconstitute(...)` (existing, `AssetTracker.Domain.Entities.Location`).
- Produces: `ILocationRepository.GetLatestForAllDevicesAsync(CancellationToken ct) : Task<IReadOnlyList<(string DeviceId, Location Location)>>`, `ILocationService.GetAllLatestLocationsAsync(CancellationToken ct) : Task<IReadOnlyList<LocationReadDto>>`, stored procedure `usp_Location_GetLatestForAllDevices` (no parameters). Task 3 (spec corrections) references this exact stored procedure name and the exclusion-of-zero-location-devices behavior below.
- Semantics: only devices with **at least one** recorded location appear in the result (an `INNER JOIN` to `devices`, not a `LEFT JOIN`) — a device that has never reported a location is simply absent from the list, not present with null fields. This resolves the join-semantics ambiguity flagged during the frontend/backend spec-alignment review.

- [ ] **Step 1: Write the new stored procedure**

Create `backend/AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_GetLatestForAllDevices.sql`:
```sql
CREATE OR ALTER PROCEDURE usp_Location_GetLatestForAllDevices
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH RankedLocations AS (
        SELECT
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
            l.created_at AS CreatedAt,
            ROW_NUMBER() OVER (PARTITION BY l.device_fk ORDER BY l.[timestamp] DESC) AS RowNum
        FROM locations l
    )
    SELECT
        r.Id,
        r.DeviceFk,
        d.device_id AS DeviceId,
        r.Timestamp,
        r.Latitude,
        r.Longitude,
        r.Altitude,
        r.Speed,
        r.Satellites,
        r.Hdop,
        r.BatteryVoltage,
        r.IsStale,
        r.CreatedAt
    FROM RankedLocations r
    INNER JOIN devices d ON d.id = r.DeviceFk
    WHERE r.RowNum = 1
    ORDER BY r.Timestamp DESC;
END
```

This file is picked up automatically by `AssetTracker.Infrastructure.csproj`'s `<None Include="Data\StoredProcedures\*.sql" CopyToOutputDirectory="PreserveNewest" />` glob — no `.csproj` edit needed.

- [ ] **Step 2: Scaffold and edit the EF Core migration**

From `backend/`, run:
```bash
dotnet tool restore
dotnet ef migrations add AddLocationGetLatestForAllDevicesProcedure --project AssetTracker.Infrastructure --startup-project AssetTracker.Api
```

This generates an empty-bodied migration (no EF model/schema changes — this is SQL-only, matching the pattern already used by `20260812233604_AddLocationStoredProcedures.cs`) at `backend/AssetTracker.Infrastructure/Data/Migrations/<timestamp>_AddLocationGetLatestForAllDevicesProcedure.cs`, where `<timestamp>` is whatever prefix the command generated. Open that file and replace its empty `Up`/`Down` bodies:

old_string (the two empty method bodies inside the generated file):
```
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
```

new_string:
```
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "StoredProcedures", "usp_Location_GetLatestForAllDevices.sql")));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_Location_GetLatestForAllDevices;");
        }
```

Do not hand-edit the accompanying `.Designer.cs` file or `AssetTrackerDbContextModelSnapshot.cs` — the CLI already generated/updated them correctly (there is no model diff to snapshot, since no `DbSet`/entity changed).

- [ ] **Step 3: Add the repository method (with its own Dapper row type, since this query needs the business-key `DeviceId` per row — unlike the other `LocationRepository` methods, which only ever operate on one already-known device)**

Add to `backend/AssetTracker.Application/Interfaces/ILocationRepository.cs`:

old_string:
```
public interface ILocationRepository
{
    Task<Location> InsertAsync(Location location, CancellationToken ct);
    Task<IReadOnlyList<Location>> BatchInsertAsync(IReadOnlyList<Location> locations, CancellationToken ct);
    Task<Location?> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
}
```

new_string:
```
public interface ILocationRepository
{
    Task<Location> InsertAsync(Location location, CancellationToken ct);
    Task<IReadOnlyList<Location>> BatchInsertAsync(IReadOnlyList<Location> locations, CancellationToken ct);
    Task<Location?> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
    Task<IReadOnlyList<(string DeviceId, Location Location)>> GetLatestForAllDevicesAsync(CancellationToken ct);
}
```

(Note: `ILocationRepository.cs` already reflects Task 1's `Location?` signature for `GetLatestByDeviceAsync` — the old_string above matches the post-Task-1 state.)

Add to `backend/AssetTracker.Infrastructure/Repositories/LocationRepository.cs`, immediately after `GetLatestByDeviceAsync` and before the closing brace of the `LocationRow` class's preceding method (i.e. right before the `private sealed class LocationRow` block):

old_string:
```
        return row?.ToEntity();
    }

    private sealed class LocationRow
```

new_string:
```
        return row?.ToEntity();
    }

    public async Task<IReadOnlyList<(string DeviceId, Location Location)>> GetLatestForAllDevicesAsync(CancellationToken ct)
    {
        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<DeviceLocationRow>(
            new CommandDefinition(
                "usp_Location_GetLatestForAllDevices",
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return rows.Select(r => (r.DeviceId, r.ToEntity())).ToList();
    }

    private sealed class LocationRow
```

Add the new private row-mapping class at the end of the `LocationRepository` class, immediately after the closing brace of `LocationRow`:

old_string:
```
        public Location ToEntity() => Location.Reconstitute(
            Id, DeviceFk, Timestamp, Latitude, Longitude, Altitude, Speed, Satellites, Hdop, BatteryVoltage, IsStale, CreatedAt);
    }
}
```

new_string:
```
        public Location ToEntity() => Location.Reconstitute(
            Id, DeviceFk, Timestamp, Latitude, Longitude, Altitude, Speed, Satellites, Hdop, BatteryVoltage, IsStale, CreatedAt);
    }

    private sealed class DeviceLocationRow
    {
        public long Id { get; set; }
        public int DeviceFk { get; set; }
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
        public DateTime CreatedAt { get; set; }

        public Location ToEntity() => Location.Reconstitute(
            Id, DeviceFk, Timestamp, Latitude, Longitude, Altitude, Speed, Satellites, Hdop, BatteryVoltage, IsStale, CreatedAt);
    }
}
```

- [ ] **Step 4: Add the service method**

Add to `backend/AssetTracker.Application/Services/ILocationService.cs`:

old_string:
```
    Task<LocationReadDto> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
}
```

new_string:
```
    Task<LocationReadDto> GetLatestByDeviceAsync(string deviceId, CancellationToken ct);
    Task<IReadOnlyList<LocationReadDto>> GetAllLatestLocationsAsync(CancellationToken ct);
}
```

Add to `backend/AssetTracker.Application/Services/LocationService.cs`, at the end of the class:

old_string:
```
            IsStale = location.IsStale
        };
    }
}
```

new_string:
```
            IsStale = location.IsStale
        };
    }

    public async Task<IReadOnlyList<LocationReadDto>> GetAllLatestLocationsAsync(CancellationToken ct)
    {
        var latestLocations = await _locationRepository.GetLatestForAllDevicesAsync(ct);

        return latestLocations.Select(x => new LocationReadDto
        {
            Id = x.Location.Id,
            DeviceId = x.DeviceId,
            Timestamp = x.Location.Timestamp,
            Latitude = x.Location.Latitude,
            Longitude = x.Location.Longitude,
            Altitude = x.Location.Altitude,
            Speed = x.Location.Speed,
            Satellites = x.Location.Satellites,
            Hdop = x.Location.Hdop,
            BatteryVoltage = x.Location.BatteryVoltage,
            IsStale = x.Location.IsStale
        }).ToList();
    }
}
```

- [ ] **Step 5: Add the `DevicesController` endpoint**

old_string:
```
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

new_string:
```
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
    private readonly ILocationService _locationService;

    public DevicesController(IDeviceService deviceService, ILocationService locationService)
    {
        _deviceService = deviceService;
        _locationService = locationService;
    }

    [HttpPost]
    public async Task<ActionResult<DeviceRegisterResponseDto>> Register([FromBody] DeviceRegisterRequestDto request, CancellationToken ct)
    {
        var response = await _deviceService.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LocationReadDto>>> GetAllLatestLocations(CancellationToken ct)
    {
        var response = await _locationService.GetAllLatestLocationsAsync(ct);
        return Ok(response);
    }
}
```

- [ ] **Step 6: Add the unit test**

Add to `backend/AssetTracker.Tests/Unit/Services/LocationServiceTests.cs`, at the end of the class:

old_string:
```
        await Assert.ThrowsAsync<LocationNotFoundException>(() => _sut.GetLatestByDeviceAsync("goat-001", CancellationToken.None));
    }
}
```

new_string:
```
        await Assert.ThrowsAsync<LocationNotFoundException>(() => _sut.GetLatestByDeviceAsync("goat-001", CancellationToken.None));
    }

    [Fact]
    public async Task GetAllLatestLocationsAsync_ReturnsMappedDtosWithDeviceIdFromRepository()
    {
        var locationA = Location.Reconstitute(1, 5, DateTimeOffset.UtcNow, 10, 20, null, null, null, null, null, false, DateTime.UtcNow);
        var locationB = Location.Reconstitute(2, 6, DateTimeOffset.UtcNow, 30, 40, null, null, null, null, null, true, DateTime.UtcNow);
        _locationRepository.Setup(r => r.GetLatestForAllDevicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string DeviceId, Location Location)>
            {
                ("goat-001", locationA),
                ("goat-002", locationB)
            });

        var result = await _sut.GetAllLatestLocationsAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("goat-001", result[0].DeviceId);
        Assert.False(result[0].IsStale);
        Assert.Equal("goat-002", result[1].DeviceId);
        Assert.True(result[1].IsStale);
    }
}
```

- [ ] **Step 7: Add the repository integration tests**

Add to `backend/AssetTracker.Tests/Integration/LocationRepositoryTests.cs`, at the end of the class:

old_string:
```
        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.Null(result);
    }
}
```

new_string:
```
        var result = await repository.GetLatestByDeviceAsync(device.DeviceId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestForAllDevicesAsync_ReturnsOneMostRecentRowPerDevice()
    {
        var deviceA = await RegisterDeviceAsync();
        var deviceB = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        await repository.InsertAsync(new Location(deviceA.Id, DateTimeOffset.UtcNow.AddMinutes(-5), 1, 1, null, null, null, null, null, false), CancellationToken.None);
        var newestA = await repository.InsertAsync(new Location(deviceA.Id, DateTimeOffset.UtcNow, 2, 2, null, null, null, null, null, false), CancellationToken.None);
        var newestB = await repository.InsertAsync(new Location(deviceB.Id, DateTimeOffset.UtcNow, 3, 3, null, null, null, null, null, true), CancellationToken.None);

        var result = await repository.GetLatestForAllDevicesAsync(CancellationToken.None);

        var resultA = Assert.Single(result, r => r.DeviceId == deviceA.DeviceId);
        var resultB = Assert.Single(result, r => r.DeviceId == deviceB.DeviceId);
        Assert.Equal(newestA.Id, resultA.Location.Id);
        Assert.Equal(newestB.Id, resultB.Location.Id);
    }

    [Fact]
    public async Task GetLatestForAllDevicesAsync_SkipsDevicesWithNoLocations()
    {
        var deviceWithLocation = await RegisterDeviceAsync();
        var deviceWithoutLocation = await RegisterDeviceAsync();
        var repository = new LocationRepository(_fixture.ConnectionString);
        await repository.InsertAsync(new Location(deviceWithLocation.Id, DateTimeOffset.UtcNow, 1, 1, null, null, null, null, null, false), CancellationToken.None);

        var result = await repository.GetLatestForAllDevicesAsync(CancellationToken.None);

        Assert.Contains(result, r => r.DeviceId == deviceWithLocation.DeviceId);
        Assert.DoesNotContain(result, r => r.DeviceId == deviceWithoutLocation.DeviceId);
    }
}
```

- [ ] **Step 8: Add the endpoint integration tests**

Add to `backend/AssetTracker.Tests/Integration/DevicesEndpointTests.cs`, at the end of the class:

old_string:
```
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("DEVICE_ALREADY_EXISTS", body!["error"].GetString());
    }
}
```

new_string:
```
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("DEVICE_ALREADY_EXISTS", body!["error"].GetString());
    }

    [Fact]
    public async Task GetAllLatestLocations_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/devices");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllLatestLocations_WithValidJwt_ReturnsOnlyDevicesWithLocations()
    {
        var token = await TestAuthHelper.GetAdminJwtAsync(_client);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var deviceWithLocationId = $"test-device-{Guid.NewGuid():N}";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/devices", new DeviceRegisterRequestDto { DeviceId = deviceWithLocationId });
        var registered = await registerResponse.Content.ReadFromJsonAsync<DeviceRegisterResponseDto>();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/locations")
        {
            Content = JsonContent.Create(new LocationCreateDto
            {
                DeviceId = deviceWithLocationId,
                Timestamp = DateTimeOffset.UtcNow,
                Latitude = 1,
                Longitude = 1
            })
        };
        createRequest.Headers.Add("X-API-Key", registered!.ApiKey);
        (await _client.SendAsync(createRequest)).EnsureSuccessStatusCode();

        var deviceWithoutLocationId = $"test-device-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/v1/devices", new DeviceRegisterRequestDto { DeviceId = deviceWithoutLocationId });

        var response = await _client.GetAsync("/api/v1/devices");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<LocationReadDto>>();
        Assert.Contains(body!, l => l.DeviceId == deviceWithLocationId);
        Assert.DoesNotContain(body!, l => l.DeviceId == deviceWithoutLocationId);
    }
}
```

- [ ] **Step 9: Run the build and unit tests**

Run (from `backend/`): `dotnet build AssetTracker.slnx`
Expected: PASS.

Run (from `backend/`): `dotnet test AssetTracker.Tests --filter "FullyQualifiedName~.Unit." --no-build`
Expected: PASS, including the new `GetAllLatestLocationsAsync_ReturnsMappedDtosWithDeviceIdFromRepository` test.

Per the Global Constraints, do not attempt to run the integration tests (no Docker here) — the build passing confirms they compile against the new members.

- [ ] **Step 10: Commit**

```bash
cd backend
git add AssetTracker.Infrastructure/Data/StoredProcedures/usp_Location_GetLatestForAllDevices.sql \
        AssetTracker.Infrastructure/Data/Migrations/ \
        AssetTracker.Application/Interfaces/ILocationRepository.cs \
        AssetTracker.Infrastructure/Repositories/LocationRepository.cs \
        AssetTracker.Application/Services/ILocationService.cs \
        AssetTracker.Application/Services/LocationService.cs \
        AssetTracker.Api/Controllers/DevicesController.cs \
        AssetTracker.Tests/Unit/Services/LocationServiceTests.cs \
        AssetTracker.Tests/Integration/LocationRepositoryTests.cs \
        AssetTracker.Tests/Integration/DevicesEndpointTests.cs
git commit -m "RT: add GET /api/v1/devices, backed by new usp_Location_GetLatestForAllDevices"
```

---

### Task 3: Correct the specs to match the implementation choices made in Tasks 1-2

**Files:**
- Modify: `specs/backend/api.md`
- Modify: `docs/superpowers/specs/2026-08-12-backend-csharp-design.md`

**Interfaces:**
- Consumes: nothing — pure documentation correction.
- Produces: nothing consumed by later tasks (this is the last task in the plan).
- Context: the specs (finalized in the prior frontend/backend spec-alignment plan) guessed at two implementation details that turned out differently once real code was written: a generic `"error": "NOT_FOUND"` code (the codebase's established convention, confirmed in Task 1, is a specific `LOCATION_NOT_FOUND`), and reuse of `usp_Location_GetLatestByDevice` for the all-devices endpoint (Task 2 found that stored procedure's migration had already shipped and — per `.clinerules/backend.md`'s migration-immutability rule — could not be altered in place, so a new, distinctly-named `usp_Location_GetLatestForAllDevices` was added instead). This task brings the docs back in sync with what was actually built, closing the loop the same way the rest of this session's work has.

- [ ] **Step 1: Fix the error code in `specs/backend/api.md`**

old_string:
```
**Error:** `404` (`"error": "NOT_FOUND"`) if the device has never reported a location. `401` (missing/invalid JWT).
```

new_string:
```
**Error:** `404` (`"error": "LOCATION_NOT_FOUND"`) if the device has never reported a location. `401` (missing/invalid JWT).
```

- [ ] **Step 2: Fix the stored procedure reference and document the join semantics in `specs/backend/api.md`**

old_string:
```
One `LocationReadDto` per registered device (its most recent location), backed by `usp_Location_GetLatestByDevice`. The dashboard derives online/offline/stale status client-side from `isStale` and timestamp age — see `specs/frontend/pages.md`.
```

new_string:
```
One `LocationReadDto` per device that has recorded at least one location (its most recent location) — a device with no location history is simply absent from the list, not present with null fields. Backed by `usp_Location_GetLatestForAllDevices` (a distinct stored procedure from `usp_Location_GetLatestByDevice` above: the two take different parameters — one device vs. all — and once a migration ships it can never be edited in place, so the all-devices query could not simply extend the existing procedure). The dashboard derives online/offline/stale status client-side from `isStale` and timestamp age — see `specs/frontend/pages.md`.
```

- [ ] **Step 3: Split the stored-procedure table row in the backend design doc**

old_string:
```
| `usp_Location_Insert` | Single location write |
| `usp_Location_BatchInsert` | Batch location write |
| `usp_Location_GetLatestByDevice` | Dashboard read — latest location per device |
| `usp_Device_Register` | Create a device, return its (hashed-and-stored) API key once |
```

new_string:
```
| `usp_Location_Insert` | Single location write |
| `usp_Location_BatchInsert` | Batch location write |
| `usp_Location_GetLatestByDevice` | Single-device read — latest location for one device |
| `usp_Location_GetLatestForAllDevices` | Dashboard list read — latest location per device, across all devices with recorded history |
| `usp_Device_Register` | Create a device, return its (hashed-and-stored) API key once |
```

- [ ] **Step 4: Fix the endpoint table's stored procedure reference in the backend design doc**

old_string:
```
| `GET` | `/api/v1/devices` | JWT (admin) | **New** — latest location per device, backed by `usp_Location_GetLatestByDevice`; powers the devices list dashboard page |
```

new_string:
```
| `GET` | `/api/v1/devices` | JWT (admin) | **New** — latest location per device, backed by `usp_Location_GetLatestForAllDevices`; powers the devices list dashboard page |
```

- [ ] **Step 5: Verify**

Run: `grep -n "usp_Location_GetLatestByDevice" specs/backend/api.md docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
Expected: exactly 1 match total — `docs/superpowers/specs/2026-08-12-backend-csharp-design.md`'s stored-procedure table row from Step 3's new_string (`| usp_Location_GetLatestByDevice | Single-device read... |`). Neither `specs/backend/api.md`'s single-device section nor the design doc's `GET /api/v1/locations/{deviceId}` endpoint-table row name the stored procedure at all, so this is the only remaining occurrence — not several.

Run: `grep -n "usp_Location_GetLatestForAllDevices" specs/backend/api.md docs/superpowers/specs/2026-08-12-backend-csharp-design.md`
Expected: 3 matches (api.md's GET /api/v1/devices note, the design doc's stored-procedure table row, the design doc's endpoint table row).

Run: `grep -n '"error": "NOT_FOUND"' specs/backend/api.md`
Expected: no output.

- [ ] **Step 6: Commit**

```bash
git add specs/backend/api.md docs/superpowers/specs/2026-08-12-backend-csharp-design.md
git commit -m "RT: sync specs with the stored-procedure split discovered during implementation"
```

---

## Self-Review Notes

- **Spec coverage:** Task 1 implements the `specs/backend/api.md` fix for `GET /api/v1/locations/{deviceId}` (singular + 404) decided during the prior spec-alignment plan. Task 2 implements `GET /api/v1/devices` per the same spec. Task 3 corrects the two spec details (error code, stored procedure name) that implementation reality changed — specifically the migration-immutability constraint in `.clinerules/backend.md`, which the original spec-writing pass did not account for because no backend code existed to consult at the time.
- **Placeholder scan:** No TBD/TODO in any task. The migration file's exact timestamp is legitimately unknowable ahead of time (it's generated from the clock at scaffold time) — Step 2 of Task 2 handles this by having the implementer scaffold it live and edit the real generated file, not by inventing a fake filename.
- **Type consistency:** `ILocationRepository.GetLatestForAllDevicesAsync`'s tuple shape `(string DeviceId, Location Location)` (Task 2, Step 3) is consumed with the exact same field names (`x.DeviceId`, `x.Location.*`) in `LocationService.GetAllLatestLocationsAsync` (Task 2, Step 4). `LocationNotFoundException` (Task 1, Step 3) is referenced identically in `ErrorHandlingMiddleware` (Step 4), `LocationService` (Step 8), and both test files (Step 1). Verified this environment can actually scaffold an EF migration and produce the expected empty-bodied file (see Step 2 of Task 2) before writing it into the plan, by running it as a throwaway probe and reverting.
