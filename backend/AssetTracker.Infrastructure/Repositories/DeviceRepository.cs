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
