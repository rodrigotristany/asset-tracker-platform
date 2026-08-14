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
