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
